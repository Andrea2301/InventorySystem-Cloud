using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Settings;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventorySystemCloud.Application.Services
{
    public class AuthService : IAuthService
    {
        private const int PasswordWorkFactor = 12;
        private const int MaximumFailedLoginAttempts = 5;
        private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("not-a-valid-user-password", workFactor: PasswordWorkFactor);
        private readonly IAppDbContext _context;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IImageStorageService _imageStorageService;
        private readonly CloudinarySettings _cloudinarySettings;

        public AuthService(
            IAppDbContext context, 
            IJwtTokenGenerator tokenGenerator,
            IEmailService emailService,
            IImageStorageService imageStorageService,
            IOptions<CloudinarySettings> cloudinarySettings)
        {
            _context = context;
            _tokenGenerator = tokenGenerator;
            _emailService = emailService;
            _imageStorageService = imageStorageService;
            _cloudinarySettings = cloudinarySettings.Value;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return ApiResponse<AuthResponseDto>.FailureResponse("Email and password are required.", statusCode: 400);

            if (!IsPasswordStrong(request.Password))
                return ApiResponse<AuthResponseDto>.FailureResponse("Password is not strong enough include special characters, numbers and letters.", statusCode: 400);

            var email = NormalizeEmail(request.Email);
            var existingUser = await _context.Users.AnyAsync(u => u.Email == email);
            if (existingUser)
                return ApiResponse<AuthResponseDto>.FailureResponse("Email already exists.", statusCode: 400);

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: PasswordWorkFactor),
                Role = UserRole.Cashier,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var responseDto = await CreateAuthResponseAsync(user, ipAddress);

            // Send Welcome Email asynchronously
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.Email);
            }
            catch
            {
                // Email delivery failure should not break user creation
            }

            return ApiResponse<AuthResponseDto>.SuccessResponse(responseDto, "Registration successful.", statusCode: 201);
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return ApiResponse<AuthResponseDto>.FailureResponse("Email and password are required.", statusCode: 401);

            var email = NormalizeEmail(request.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            var passwordIsValid = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyPasswordHash);

            if (user == null || !passwordIsValid)
            {
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= MaximumFailedLoginAttempts)
                    {
                        user.FailedLoginAttempts = 0;
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    }

                    await _context.SaveChangesAsync();
                }

                return ApiResponse<AuthResponseDto>.FailureResponse("Email and password are incorrect.", statusCode: 401);
            }

            if (!user.IsActive || (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow))
                return ApiResponse<AuthResponseDto>.FailureResponse("Email and password are incorrect.", statusCode: 401);

            user.LastLogin = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            var responseDto = await CreateAuthResponseAsync(user, ipAddress);
            await _context.SaveChangesAsync();

            return ApiResponse<AuthResponseDto>.SuccessResponse(responseDto, "Login successful.");
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return ApiResponse<AuthResponseDto>.FailureResponse("Refresh token is required.", statusCode: 400);

            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null)
                return ApiResponse<AuthResponseDto>.FailureResponse("Invalid refresh token.", statusCode: 401);

            // Security: Refresh token reuse detection (stolen token mitigation)
            if (refreshToken.IsRevoked)
            {
                if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
                {
                    // Revoke all descendant active tokens for this user
                    var activeTokens = await _context.RefreshTokens
                        .Where(rt => rt.UserId == refreshToken.UserId && rt.RevokedAt == null)
                        .ToListAsync();

                    foreach (var token in activeTokens)
                    {
                        token.RevokedAt = DateTime.UtcNow;
                        token.RevokedByIp = ipAddress;
                    }

                    await _context.SaveChangesAsync();
                }

                return ApiResponse<AuthResponseDto>.FailureResponse("Revoked refresh token.", statusCode: 401);
            }

            if (refreshToken.IsExpired)
                return ApiResponse<AuthResponseDto>.FailureResponse("Refresh token has expired.", statusCode: 401);

            var user = refreshToken.User;
            if (user == null || !user.IsActive)
                return ApiResponse<AuthResponseDto>.FailureResponse("User not found or inactive.", statusCode: 401);

            // Rotate Refresh Token
            var newRefreshToken = _tokenGenerator.GenerateRefreshToken(user.Id, ipAddress);
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var newAccessToken = _tokenGenerator.GenerateToken(user);

            var responseDto = new AuthResponseDto
            {
                Token = newAccessToken.Value,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
                Email = user.Email,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl,
                ExpiresAt = newAccessToken.ExpiresAt
            };

            return ApiResponse<AuthResponseDto>.SuccessResponse(responseDto, "Token refreshed successfully.");
        }

        public async Task<ApiResponse<string>> RevokeTokenAsync(RevokeTokenRequestDto request, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return ApiResponse<string>.FailureResponse("Refresh token is required.", statusCode: 400);

            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
            if (refreshToken == null || refreshToken.IsRevoked)
                return ApiResponse<string>.FailureResponse("Token not found or already revoked.", statusCode: 400);

            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse("Token revoked successfully.", "Token revoked successfully.");
        }

        public async Task<ApiResponse<string>> LogoutAsync(Guid publicId)
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.PublicId == publicId);

            if (user == null)
                return ApiResponse<string>.FailureResponse("User not found.", statusCode: 404);

            user.SecurityStamp = Guid.NewGuid().ToString("N");

            // Revoke all active refresh tokens on logout
            foreach (var rt in user.RefreshTokens.Where(rt => rt.IsActive))
            {
                rt.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse("Logout successful.", "Logout successful.");
        }

        public async Task<ApiResponse<string>> UploadAvatarAsync(Guid publicId, Stream fileStream, string fileName)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
                return ApiResponse<string>.FailureResponse("User not found.", statusCode: 404);

            if (!string.IsNullOrEmpty(user.AvatarPublicId))
            {
                await _imageStorageService.DeleteImageAsync(user.AvatarPublicId);
            }

            var folder = !string.IsNullOrEmpty(_cloudinarySettings.AvatarFolder)
                ? _cloudinarySettings.AvatarFolder
                : "inventory/avatars";

            var uploadResult = await _imageStorageService.UploadImageAsync(fileStream, fileName, folder);
            if (!uploadResult.Success)
            {
                return ApiResponse<string>.FailureResponse(
                    uploadResult.ErrorMessage ?? "Error uploading avatar to Cloudinary.",
                    statusCode: 500);
            }

            user.AvatarUrl = uploadResult.Url;
            user.AvatarPublicId = uploadResult.PublicId;

            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse(user.AvatarUrl ?? string.Empty, "Avatar uploaded successfully.");
        }

        public async Task<ApiResponse<string>> DeleteAvatarAsync(Guid publicId)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
                return ApiResponse<string>.FailureResponse("User not found.", statusCode: 404);

            if (!string.IsNullOrEmpty(user.AvatarPublicId))
            {
                await _imageStorageService.DeleteImageAsync(user.AvatarPublicId);
            }

            user.AvatarUrl = null;
            user.AvatarPublicId = null;

            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse("Avatar deleted successfully.", "Avatar deleted successfully.");
        }

        private async Task<AuthResponseDto> CreateAuthResponseAsync(User user, string? ipAddress)
        {
            var token = _tokenGenerator.GenerateToken(user);
            var refreshToken = _tokenGenerator.GenerateRefreshToken(user.Id, ipAddress);

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token.Value,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                Email = user.Email,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl,
                ExpiresAt = token.ExpiresAt
            };
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static bool IsPasswordStrong(string password) =>
            password.Length is >= 12 and <= 128 &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsLower) &&
            password.Any(char.IsDigit) &&
            password.Any(c => !char.IsLetterOrDigit(c));
    }
}
