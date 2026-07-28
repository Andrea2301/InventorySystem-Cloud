using System;
using System.Threading.Tasks;
using FluentAssertions;
using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
            mock.Setup(g => g.GenerateToken(It.IsAny<User>())).Returns("fake_jwt_token_12345");
            return mock;
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsSuccessAndToken()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();
            var authService = new AuthService(context, mockTokenGen.Object);

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

            var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Email == "testuser@bakery.com");
            userInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();

            context.Users.Add(new User
            {
                Email = "existing@bakery.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var authService = new AuthService(context, mockTokenGen.Object);

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
            result.Message.Should().Contain("Ya existe un usuario");
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccessAndToken()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();

            var rawPassword = "SecurePassword123";
            context.Users.Add(new User
            {
                Email = "loginuser@bakery.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var authService = new AuthService(context, mockTokenGen.Object);

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
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockTokenGen = GetMockTokenGenerator();

            context.Users.Add(new User
            {
                Email = "loginuser2@bakery.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123"),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var authService = new AuthService(context, mockTokenGen.Object);

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
            result.Message.Should().Contain("Credenciales incorrectas");
        }
    }
}
