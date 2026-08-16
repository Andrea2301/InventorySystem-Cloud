using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventorySystemCloud.UnitTests.Services
{
    public class SaleServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateSaleAsync_WithValidData_DeductsStockAndCreatesSale()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockAudit = new Mock<IAuditService>();

            var user = new User { Email = "cashier@test.com", PasswordHash = "hash", IsActive = true };
            var client = new Client { DocumentNumber = "12345678", FirstName = "Mario", LastName = "Bros", Email = "mario@test.com", PhoneNumber = "123", IsActive = true };
            var product = new Product { Name = "Harina 1kg", Category = "Insumos", Price = 20.00m, Quantity = 50, IsActive = true };

            context.Users.Add(user);
            context.Clients.Add(client);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new SaleService(context, mockAudit.Object);

            var request = new CreateSaleDto
            {
                ClientId = client.Id,
                PaymentMethod = "Efectivo",
                AmountPaid = 100.00m,
                Currency = "COP",
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto { ProductId = product.Id, Quantity = 2 } // Total = 40.00
                }
            };

            // Act
            var result = await service.CreateSaleAsync(request, user.PublicId);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data.Should().NotBeNull();
            result.Data!.TotalAmount.Should().Be(40.00m);
            result.Data.AmountPaid.Should().Be(100.00m);
            result.Data.ChangeDue.Should().Be(60.00m);
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().ProductName.Should().Be("Harina 1kg");

            // Verify stock deducted in DB
            var productInDb = await context.Products.FindAsync(product.Id);
            productInDb!.Quantity.Should().Be(48); // 50 - 2

            // Verify audit logging was called
            mockAudit.Verify(a => a.LogActionAsync(user.Id, "CREATE_SALE", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateSaleAsync_WithInsufficientStock_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockAudit = new Mock<IAuditService>();

            var user = new User { Email = "cashier@test.com", PasswordHash = "hash", IsActive = true };
            var client = new Client { DocumentNumber = "12345678", FirstName = "Luigi", LastName = "Bros", Email = "luigi@test.com", PhoneNumber = "123", IsActive = true };
            var product = new Product { Name = "Levadura", Category = "Insumos", Price = 5.00m, Quantity = 3, IsActive = true };

            context.Users.Add(user);
            context.Clients.Add(client);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new SaleService(context, mockAudit.Object);

            var request = new CreateSaleDto
            {
                ClientId = client.Id,
                PaymentMethod = "Efectivo",
                AmountPaid = 50.00m,
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto { ProductId = product.Id, Quantity = 10 } // Demands 10, stock is only 3
                }
            };

            // Act
            var result = await service.CreateSaleAsync(request, user.PublicId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("Stock insuficiente");

            // Stock should remain untouched
            var productInDb = await context.Products.FindAsync(product.Id);
            productInDb!.Quantity.Should().Be(3);
        }

        [Fact]
        public async Task CreateSaleAsync_WithAmountPaidLessThanTotal_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockAudit = new Mock<IAuditService>();

            var user = new User { Email = "cashier@test.com", PasswordHash = "hash", IsActive = true };
            var client = new Client { DocumentNumber = "12345678", FirstName = "Peach", LastName = "Princess", Email = "peach@test.com", PhoneNumber = "123", IsActive = true };
            var product = new Product { Name = "Pastel", Category = "Repostería", Price = 100.00m, Quantity = 10, IsActive = true };

            context.Users.Add(user);
            context.Clients.Add(client);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new SaleService(context, mockAudit.Object);

            var request = new CreateSaleDto
            {
                ClientId = client.Id,
                PaymentMethod = "Efectivo",
                AmountPaid = 50.00m, // Total is 100, but only paying 50
                Items = new List<CreateSaleItemDto>
                {
                    new CreateSaleItemDto { ProductId = product.Id, Quantity = 1 }
                }
            };

            // Act
            var result = await service.CreateSaleAsync(request, user.PublicId);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Message.Should().Contain("menor que el total");
        }

        [Fact]
        public async Task GetDailyReportAsync_ComputesTotalsCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var mockAudit = new Mock<IAuditService>();

            var today = DateTime.UtcNow;
            var sale1 = new Sale
            {
                SaleDate = today,
                TotalAmount = 100m,
                SaleDetails = new List<SaleDetail>
                {
                    new SaleDetail { Quantity = 2, UnitPrice = 50m, TotalPrice = 100m }
                }
            };

            var sale2 = new Sale
            {
                SaleDate = today,
                TotalAmount = 200m,
                SaleDetails = new List<SaleDetail>
                {
                    new SaleDetail { Quantity = 4, UnitPrice = 50m, TotalPrice = 200m }
                }
            };

            context.Sales.AddRange(sale1, sale2);
            await context.SaveChangesAsync();

            var service = new SaleService(context, mockAudit.Object);

            // Act
            var result = await service.GetDailyReportAsync(today);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalSalesCount.Should().Be(2);
            result.Data.TotalRevenue.Should().Be(300m);
            result.Data.AverageTicket.Should().Be(150m);
            result.Data.TotalItemsSold.Should().Be(6);
        }
    }
}
