using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using InventorySystemCloud.Application.DTOs.Users;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Domain.Constants;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventorySystemCloud.UnitTests.Services
{
    public class UserServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateCashierAsync_WithValidData_CreatesUserWithPermissions()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockEmail = new Mock<IEmailService>();
            var service = new UserService(context, mockEmail.Object);

            var request = new CreateCashierDto
            {
                Email = "cashier1@store.com",
                Password = "Password123*!",
                Permissions = new List<string> { AppPermissions.Sales.Create, AppPermissions.Products.View }
            };

            // Act
            var result = await service.CreateCashierAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNull();
            result.Data!.Email.Should().Be("cashier1@store.com");
            result.Data.Role.Should().Be("Cashier");
            result.Data.Permissions.Should().Contain(AppPermissions.Sales.Create);
            result.Data.Permissions.Should().Contain(AppPermissions.Products.View);

            var userInDb = await context.Users.Include(u => u.Permissions).FirstOrDefaultAsync(u => u.Email == "cashier1@store.com");
            userInDb.Should().NotBeNull();
            userInDb!.Permissions.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdatePermissionsAsync_UpdatesPermissionsAndRotatesSecurityStamp()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockEmail = new Mock<IEmailService>();
            var service = new UserService(context, mockEmail.Object);

            var user = new User
            {
                Email = "cashier2@store.com",
                PasswordHash = "hash",
                Role = UserRole.Cashier,
                IsActive = true,
                SecurityStamp = "initial_stamp"
            };
            user.Permissions.Add(new UserPermission { Permission = AppPermissions.Sales.View, User = user });

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var updateDto = new UpdateUserPermissionsDto
            {
                Permissions = new List<string> { AppPermissions.Products.Create, AppPermissions.Clients.Create }
            };

            // Act
            var result = await service.UpdatePermissionsAsync(user.Id, updateDto);

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.Permissions.Should().BeEquivalentTo(new[] { AppPermissions.Products.Create, AppPermissions.Clients.Create });

            var updatedUser = await context.Users.Include(u => u.Permissions).FirstOrDefaultAsync(u => u.Id == user.Id);
            updatedUser!.SecurityStamp.Should().NotBe("initial_stamp");
            updatedUser.Permissions.Should().HaveCount(2);
        }

        [Fact]
        public async Task ToggleStatusAsync_DeactivatesUserAndRotatesSecurityStamp()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockEmail = new Mock<IEmailService>();
            var service = new UserService(context, mockEmail.Object);

            var user = new User
            {
                Email = "cashier3@store.com",
                PasswordHash = "hash",
                Role = UserRole.Cashier,
                IsActive = true,
                SecurityStamp = "stamp_123"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await service.ToggleStatusAsync(user.Id, false);

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.IsActive.Should().BeFalse();

            var updatedUser = await context.Users.FindAsync(user.Id);
            updatedUser!.IsActive.Should().BeFalse();
            updatedUser.SecurityStamp.Should().NotBe("stamp_123");
        }

        [Fact]
        public void GetPermissionsCatalog_ReturnsAllSystemModules()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockEmail = new Mock<IEmailService>();
            var service = new UserService(context, mockEmail.Object);

            // Act
            var result = service.GetPermissionsCatalog();

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            result.Data.Should().Contain(g => g.Module == "Productos");
            result.Data.Should().Contain(g => g.Module == "Ventas");
            result.Data.Should().Contain(g => g.Module == "Clientes");
        }
    }
}
