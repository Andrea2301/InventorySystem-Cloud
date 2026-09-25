using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Application.DTOs.Images;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Application.Settings;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace InventorySystemCloud.UnitTests.Services
{
    public class AuthServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private Mock<IJwtTokenGenerator> GetMockTokenGenerator()
        {
            var mock = new Mock<IJwtTokenGenerator>();
            mock.Setup(g => g.GenerateToken(It.IsAny<User>())).Returns(new GeneratedToken
            {
                Value = "fake_jwt_token_12345",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });
            mock.Setup(g => g.GenerateRefreshToken(It.IsAny<int>(), It.IsAny<string>())).Returns((int userId, string? ip) => new RefreshToken
            {
                Token = Guid.NewGuid().ToString("N"),
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ip
            });
            return mock;
        }

        private AuthService CreateService(
            AppDbContext context,
            Mock<IJwtTokenGenerator>? mockTokenGen = null,
            Mock<IEmailService>? mockEmail = null,
            Mock<IImageStorageService>? mockImageStorage = null,
            CloudinarySettings? settings = null)
        {
            var tokenGen = mockTokenGen?.Object ?? GetMockTokenGenerator().Object;
            var email = mockEmail?.Object ?? new Mock<IEmailService>().Object;
            var imageStorage = mockImageStorage?.Object ?? new Mock<IImageStorageService>().Object;
            var options = Options.Create(settings ?? new CloudinarySettings());

            return new AuthService(context, tokenGen, email, imageStorage, options);
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsSuccessAndToken_AndSendsWelcomeEmail()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();
            var mockEmail = new Mock<IEmailService>();
            var authService = CreateService(context, mockTokenGen, mockEmail);

            var request = new RegisterRequestDto
            {
                Email = "testuser@bakery.com",
                Password = "Password123!"
            };

            // Act
            var result = await authService.RegisterAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNull();
            result.Data!.Email.Should().Be("testuser@bakery.com");
            result.Data.Token.Should().Be("fake_jwt_token_12345");
            result.Data.RefreshToken.Should().NotBeNullOrEmpty();
            mockEmail.Verify(e => e.SendWelcomeEmailAsync("testuser@bakery.com", "testuser@bakery.com"), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithWeakPassword_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();
            var mockEmail = new Mock<IEmailService>();
            var authService = CreateService(context, mockTokenGen, mockEmail);

            var request = new RegisterRequestDto
            {
                Email = "weakpwd@bakery.com",
                Password = "weak"
            };

            // Act
            var result = await authService.RegisterAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Password is not strong enough");
            mockEmail.Verify(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();
            var mockEmail = new Mock<IEmailService>();

            context.Users.Add(new User
            {
                Email = "existing@bakery.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var authService = CreateService(context, mockTokenGen, mockEmail);

            var request = new RegisterRequestDto
            {
                Email = "existing@bakery.com",
                Password = "Password123!"
            };

            // Act
            var result = await authService.RegisterAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Email already exists");
            mockEmail.Verify(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccessAndToken()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();
            var mockEmail = new Mock<IEmailService>();

            var rawPassword = "SecurePassword123";
            context.Users.Add(new User
            {
                Email = "loginuser@bakery.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var authService = CreateService(context, mockTokenGen, mockEmail);

            var request = new LoginRequestDto
            {
                Email = "loginuser@bakery.com",
                Password = rawPassword
            };

            // Act
            var result = await authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().Be("fake_jwt_token_12345");
            result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();
            var mockEmail = new Mock<IEmailService>();

            context.Users.Add(new User
            {
                Email = "loginuser2@bakery.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123"),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var authService = CreateService(context, mockTokenGen, mockEmail);

            var request = new LoginRequestDto
            {
                Email = "loginuser2@bakery.com",
                Password = "WrongPassword"
            };

            // Act
            var result = await authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(401);
            result.Message.Should().Contain("Email and password are incorrect");
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidActiveToken_RotatesTokenAndReturnsSuccess()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();
            var mockEmail = new Mock<IEmailService>();

            var user = new User { Email = "refresh@test.com", PasswordHash = "hash", IsActive = true };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var initialToken = new RefreshToken
            {
                Token = "valid_refresh_token_xyz",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            context.RefreshTokens.Add(initialToken);
            await context.SaveChangesAsync();

            var authService = CreateService(context, mockTokenGen, mockEmail);

            // Act
            var result = await authService.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "valid_refresh_token_xyz" });

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(200);
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().Be("fake_jwt_token_12345");
            result.Data.RefreshToken.Should().NotBe("valid_refresh_token_xyz");

            // Old token must be revoked
            var oldToken = await context.RefreshTokens.FirstAsync(rt => rt.Token == "valid_refresh_token_xyz");
            oldToken.IsRevoked.Should().BeTrue();
            oldToken.ReplacedByToken.Should().Be(result.Data.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithRevokedReusedToken_RevokesAllUserTokens()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();
            var mockEmail = new Mock<IEmailService>();

            var user = new User { Email = "compromised@test.com", PasswordHash = "hash", IsActive = true };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var compromisedToken = new RefreshToken
            {
                Token = "stolen_token",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                RevokedAt = DateTime.UtcNow.AddHours(-1),
                ReplacedByToken = "legit_new_token"
            };

            var legitNewToken = new RefreshToken
            {
                Token = "legit_new_token",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            context.RefreshTokens.AddRange(compromisedToken, legitNewToken);
            await context.SaveChangesAsync();

            var authService = CreateService(context, mockTokenGen, mockEmail);

            // Act
            var result = await authService.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "stolen_token" });

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(401);

            // All tokens for user must now be revoked
            var activeTokens = await context.RefreshTokens.CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null);
            activeTokens.Should().Be(0);
        }

        [Fact]
        public async Task UploadAvatarAsync_WithValidUser_UploadsAvatarAndUpdatesUser()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var user = new User { Email = "avataruser@test.com", PasswordHash = "hash", IsActive = true };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mockStorage = new Mock<IImageStorageService>();
            mockStorage
                .Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), "avatar.png", "inventory/avatars"))
                .ReturnsAsync(ImageUploadResult.Succeeded("https://res.cloudinary.com/demo/image/upload/v1/avatar.png", "inventory/avatars/user_123"));

            var authService = CreateService(context, mockImageStorage: mockStorage);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("avatar-image"));

            // Act
            var result = await authService.UploadAvatarAsync(user.PublicId, stream, "avatar.png");

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().Be("https://res.cloudinary.com/demo/image/upload/v1/avatar.png");

            var inDb = await context.Users.FindAsync(user.Id);
            inDb!.AvatarUrl.Should().Be("https://res.cloudinary.com/demo/image/upload/v1/avatar.png");
            inDb.AvatarPublicId.Should().Be("inventory/avatars/user_123");
        }
    }
}
