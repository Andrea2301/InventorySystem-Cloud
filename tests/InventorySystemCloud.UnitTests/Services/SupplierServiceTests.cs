using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InventorySystemCloud.Application.DTOs.Suppliers;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventorySystemCloud.UnitTests.Services
{
    public class SupplierServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsActiveSuppliers_ByDefault()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Suppliers.AddRange(
                new Supplier { CompanyName = "Distribuidora A", Email = "a@dist.com", PhoneNumber = "123", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Supplier { CompanyName = "Distribuidora B", Email = "b@dist.com", PhoneNumber = "456", IsActive = false, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = new SupplierService(context);

            // Act
            var result = await service.GetAllAsync(includeInactive: false);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data!.First().CompanyName.Should().Be("Distribuidora A");
        }

        [Fact]
        public async Task CreateAsync_WithValidData_CreatesSupplier()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new SupplierService(context);

            var request = new CreateSupplierDto
            {
                CompanyName = "Lácteos del Sur",
                Email = "contacto@lacteosdelsur.com",
                PhoneNumber = "+51999888777",
                Category = "Lácteos"
            };

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data!.CompanyName.Should().Be("Lácteos del Sur");

            var inDb = await context.Suppliers.FirstOrDefaultAsync(s => s.CompanyName == "Lácteos del Sur");
            inDb.Should().NotBeNull();
            inDb!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateEmail_ReturnsConflict()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Suppliers.Add(new Supplier { CompanyName = "Empresa 1", Email = "rep@test.com", PhoneNumber = "1", IsActive = true, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new SupplierService(context);
            var request = new CreateSupplierDto { CompanyName = "Empresa 2", Email = "rep@test.com", PhoneNumber = "2" };

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task DeleteAsync_WithExistingId_SoftDeletesSupplier()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var supplier = new Supplier { CompanyName = "Para Desactivar", Email = "desact@test.com", PhoneNumber = "000", IsActive = true, CreatedAt = DateTime.UtcNow };
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            var service = new SupplierService(context);

            // Act
            var result = await service.DeleteAsync(supplier.Id);

            // Assert
            result.Success.Should().BeTrue();

            var inDb = await context.Suppliers.FindAsync(supplier.Id);
            inDb!.IsActive.Should().BeFalse();
        }
    }
}
