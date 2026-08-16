using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InventorySystemCloud.Application.DTOs.Products;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventorySystemCloud.UnitTests.Services
{
    public class ProductServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOnlyActiveProducts_ByDefault()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Products.AddRange(
                new Product { Name = "Activo", Category = "A", Price = 10, Quantity = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Name = "Inactivo", Category = "A", Price = 5, Quantity = 0, IsActive = false, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = new ProductService(context);

            // Act
            var result = await service.GetAllAsync(includeInactive: false);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data!.First().Name.Should().Be("Activo");
        }

        [Fact]
        public async Task GetAllAsync_WithIncludeInactive_ReturnsAllProducts()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Products.AddRange(
                new Product { Name = "Activo", Category = "A", Price = 10, Quantity = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Product { Name = "Inactivo", Category = "A", Price = 5, Quantity = 0, IsActive = false, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = new ProductService(context);

            // Act
            var result = await service.GetAllAsync(includeInactive: true);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ReturnsProduct()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var product = new Product { Name = "Test", Category = "A", Price = 100, Quantity = 10, IsActive = true, CreatedAt = DateTime.UtcNow };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new ProductService(context);

            // Act
            var result = await service.GetByIdAsync(product.Id);

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.Name.Should().Be("Test");
            result.Data.Price.Should().Be(100);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new ProductService(context);

            // Act
            var result = await service.GetByIdAsync(999);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task CreateAsync_WithValidData_CreatesProduct()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new ProductService(context);

            var request = new CreateProductDto
            {
                Name = "Nuevo Producto",
                Category = "Electrónica",
                Price = 299.99m,
                Quantity = 50
            };

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data!.Name.Should().Be("Nuevo Producto");
            result.Data.Price.Should().Be(299.99m);

            var inDb = await context.Products.FirstOrDefaultAsync(p => p.Name == "Nuevo Producto");
            inDb.Should().NotBeNull();
            inDb!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateName_ReturnsConflict()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Products.Add(new Product { Name = "Producto Existente", Category = "A", Price = 10, Quantity = 5, IsActive = true, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ProductService(context);
            var request = new CreateProductDto { Name = "Producto Existente", Category = "A", Price = 20, Quantity = 1 };

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task UpdateAsync_WithValidData_UpdatesProduct()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var product = new Product { Name = "Original", Category = "A", Price = 50, Quantity = 10, IsActive = true, CreatedAt = DateTime.UtcNow };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new ProductService(context);
            var request = new UpdateProductDto { Name = "Actualizado", Category = "B", Price = 75, Quantity = 20, IsActive = true };

            // Act
            var result = await service.UpdateAsync(product.Id, request);

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.Name.Should().Be("Actualizado");
            result.Data.Price.Should().Be(75);
            result.Data.Category.Should().Be("B");
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new ProductService(context);
            var request = new UpdateProductDto { Name = "X", Category = "A", Price = 10, Quantity = 1 };

            // Act
            var result = await service.UpdateAsync(999, request);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task DeleteAsync_WithExistingId_SoftDeletesProduct()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var product = new Product { Name = "Para Borrar", Category = "A", Price = 10, Quantity = 5, IsActive = true, CreatedAt = DateTime.UtcNow };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new ProductService(context);

            // Act
            var result = await service.DeleteAsync(product.Id);

            // Assert
            result.Success.Should().BeTrue();

            var inDb = await context.Products.FindAsync(product.Id);
            inDb!.IsActive.Should().BeFalse(); // Soft delete — still in DB
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new ProductService(context);

            // Act
            var result = await service.DeleteAsync(999);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
        }
    }
}
