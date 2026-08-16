using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InventorySystemCloud.Application.DTOs.Purchases;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventorySystemCloud.UnitTests.Services
{
    public class PurchaseServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreatePurchaseAsync_WithValidData_IncreasesStockAndCreatesPurchase()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockAudit = new Mock<IAuditService>();

            var user = new User { Email = "buyer@test.com", PasswordHash = "hash", IsActive = true };
            var supplier = new Supplier { CompanyName = "Molinos del Valle", Email = "molinos@test.com", PhoneNumber = "3001234567", IsActive = true };
            var product = new Product { Name = "Harina 25kg", Category = "Insumos", Price = 120.00m, Quantity = 10, IsActive = true };

            context.Users.Add(user);
            context.Suppliers.Add(supplier);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new PurchaseService(context, mockAudit.Object);

            var request = new CreatePurchaseDto
            {
                SupplierId = supplier.Id,
                InvoiceNumber = "FAC-9876",
                Notes = "Pedido quincenal",
                Currency = "COP",
                Items = new List<CreatePurchaseItemDto>
                {
                    new CreatePurchaseItemDto { ProductId = product.Id, Quantity = 15, UnitPrice = 80.00m }
                }
            };

            // Act
            var result = await service.CreatePurchaseAsync(request, user.PublicId);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNull();
            result.Data!.SupplierName.Should().Be("Molinos del Valle");
            result.Data.TotalAmount.Should().Be(1200.00m); // 15 * 80
            result.Data.InvoiceNumber.Should().Be("FAC-9876");
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().ProductName.Should().Be("Harina 25kg");

            // Verify stock increased in DB (10 + 15 = 25)
            var productInDb = await context.Products.FindAsync(product.Id);
            productInDb!.Quantity.Should().Be(25);

            // Verify audit logging was called
            mockAudit.Verify(a => a.LogActionAsync(user.Id, "CREATE_PURCHASE", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreatePurchaseAsync_WithInvalidSupplier_ReturnsNotFound()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockAudit = new Mock<IAuditService>();

            var user = new User { Email = "buyer@test.com", PasswordHash = "hash", IsActive = true };
            var product = new Product { Name = "Azúcar", Category = "Insumos", Price = 30.00m, Quantity = 5, IsActive = true };

            context.Users.Add(user);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new PurchaseService(context, mockAudit.Object);

            var request = new CreatePurchaseDto
            {
                SupplierId = 999, // Inexistente
                Items = new List<CreatePurchaseItemDto>
                {
                    new CreatePurchaseItemDto { ProductId = product.Id, Quantity = 5, UnitPrice = 20.00m }
                }
            };

            // Act
            var result = await service.CreatePurchaseAsync(request, user.PublicId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            result.Message.Should().Contain("proveedor especificado no existe");
        }

        [Fact]
        public async Task CreatePurchaseAsync_WithInactiveSupplier_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockAudit = new Mock<IAuditService>();

            var user = new User { Email = "buyer@test.com", PasswordHash = "hash", IsActive = true };
            var supplier = new Supplier { CompanyName = "Inactivos SA", Email = "inactivo@test.com", PhoneNumber = "111", IsActive = false };
            var product = new Product { Name = "Azúcar", Category = "Insumos", Price = 30.00m, Quantity = 5, IsActive = true };

            context.Users.Add(user);
            context.Suppliers.Add(supplier);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new PurchaseService(context, mockAudit.Object);

            var request = new CreatePurchaseDto
            {
                SupplierId = supplier.Id,
                Items = new List<CreatePurchaseItemDto>
                {
                    new CreatePurchaseItemDto { ProductId = product.Id, Quantity = 5, UnitPrice = 20.00m }
                }
            };

            // Act
            var result = await service.CreatePurchaseAsync(request, user.PublicId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("se encuentra inactivo");
        }

        [Fact]
        public async Task CreatePurchaseAsync_WithZeroQuantity_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockAudit = new Mock<IAuditService>();

            var user = new User { Email = "buyer@test.com", PasswordHash = "hash", IsActive = true };
            var supplier = new Supplier { CompanyName = "Distribuidora SAS", Email = "dist@test.com", PhoneNumber = "222", IsActive = true };
            var product = new Product { Name = "Sal", Category = "Insumos", Price = 10.00m, Quantity = 5, IsActive = true };

            context.Users.Add(user);
            context.Suppliers.Add(supplier);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new PurchaseService(context, mockAudit.Object);

            var request = new CreatePurchaseDto
            {
                SupplierId = supplier.Id,
                Items = new List<CreatePurchaseItemDto>
                {
                    new CreatePurchaseItemDto { ProductId = product.Id, Quantity = 0, UnitPrice = 10.00m }
                }
            };

            // Act
            var result = await service.CreatePurchaseAsync(request, user.PublicId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("debe ser mayor a cero");
        }

        [Fact]
        public async Task GetDailyReportAsync_ComputesTotalsCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockAudit = new Mock<IAuditService>();

            var today = DateTime.UtcNow;
            var purchase1 = new Purchase
            {
                PurchaseDate = today,
                TotalAmount = 500m,
                PurchaseDetails = new List<PurchaseDetail>
                {
                    new PurchaseDetail { Quantity = 5, UnitPrice = 100m, TotalPrice = 500m }
                }
            };

            var purchase2 = new Purchase
            {
                PurchaseDate = today,
                TotalAmount = 300m,
                PurchaseDetails = new List<PurchaseDetail>
                {
                    new PurchaseDetail { Quantity = 3, UnitPrice = 100m, TotalPrice = 300m }
                }
            };

            context.Purchases.AddRange(purchase1, purchase2);
            await context.SaveChangesAsync();

            var service = new PurchaseService(context, mockAudit.Object);

            // Act
            var result = await service.GetDailyReportAsync(today);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalPurchasesCount.Should().Be(2);
            result.Data.TotalSpent.Should().Be(800m);
            result.Data.AveragePurchaseCost.Should().Be(400m);
            result.Data.TotalItemsPurchased.Should().Be(8);
        }
    }
}
