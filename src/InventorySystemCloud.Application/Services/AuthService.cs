using System;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Application.Services
{
    public class AuthService : IAuthService
    {
        private const int PasswordWorkFactor = 12;
        private const int MaximumFailedLoginAttempts = 5;
        private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("not-a-valid-user-password", workFactor: PasswordWorkFactor);
        private readonly IAppDbContext _context;
        private readonly IJwtTokenGenerator _tokenGenerator;

        public AuthService(IAppDbContext context, IJwtTokenGenerator tokenGenerator)
        {
            _context = context;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return ApiResponse<AuthResponseDto>.FailureResponse("El correo electrónico y la contraseña son obligatorios.", statusCode: 400);

            if (!IsPasswordStrong(request.Password))
                return ApiResponse<AuthResponseDto>.FailureResponse("La contraseña debe tener entre 12 y 128 caracteres e incluir mayúscula, minúscula, número y símbolo.", statusCode: 400);

            var email = NormalizeEmail(request.Email);
            var existingUser = await _context.Users.AnyAsync(u => u.Email == email);
            if (existingUser)
                return ApiResponse<AuthResponseDto>.FailureResponse("No fue posible completar el registro.", statusCode: 400);

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

            return ApiResponse<AuthResponseDto>.SuccessResponse(CreateAuthResponse(user), "Registro exitoso.", statusCode: 201);
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return ApiResponse<AuthResponseDto>.FailureResponse("Credenciales incorrectas.", statusCode: 401);

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

                return ApiResponse<AuthResponseDto>.FailureResponse("Credenciales incorrectas.", statusCode: 401);
            }

            if (!user.IsActive || (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow))
                return ApiResponse<AuthResponseDto>.FailureResponse("Credenciales incorrectas.", statusCode: 401);

            user.LastLogin = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();

            return ApiResponse<AuthResponseDto>.SuccessResponse(CreateAuthResponse(user), "Inicio de sesión exitoso.");
        }

        public async Task<ApiResponse<string>> LogoutAsync(Guid publicId)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
                return ApiResponse<string>.FailureResponse("Usuario no encontrado.", statusCode: 404);

            user.SecurityStamp = Guid.NewGuid().ToString("N");
            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse("Cierre de sesión exitoso.", "Cierre de sesión exitoso.");
        }

        private AuthResponseDto CreateAuthResponse(User user)
        {
            var token = _tokenGenerator.GenerateToken(user);
            return new AuthResponseDto
            {
                Token = token.Value,
                Email = user.Email,
                Role = user.Role.ToString(),
                ExpiresAt = token.ExpiresAt
            };
        }

        /// <summary>
        /// Normalizes an email address: trims whitespace and converts to lowercase.
        /// Ensures consistent storage and lookup without relying on DB-level case folding.
        /// </summary>
        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static bool IsPasswordStrong(string password) =>
            password.Length is >= 12 and <= 128 &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsLower) &&
            password.Any(char.IsDigit) &&
            password.Any(c => !char.IsLetterOrDigit(c));
    }
}
