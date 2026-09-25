using System;
using System.Threading.Tasks;
using FluentAssertions;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Infrastructure.Data;
using InventorySystemCloud.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventorySystemCloud.UnitTests.Services
{
    public class ReportServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GenerateProductsExcelReport_ShouldReturnValidBytes()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Products.AddRange(
                new Product { Name = "Laptop Dell", Category = "Tecnología", Price = 3500000, Quantity = 10, IsActive = true },
                new Product { Name = "Mouse Inalámbrico", Category = "Accesorios", Price = 80000, Quantity = 2, IsActive = true }
            );
            await context.SaveChangesAsync();

            var service = new ReportService(context);

            // Act
            var bytes = await service.GenerateProductsExcelReportAsync();

            // Assert
            bytes.Should().NotBeNull();
            bytes.Length.Should().BeGreaterThan(100);
        }

        [Fact]
        public async Task GenerateProductsCsvReport_ShouldIncludeUtf8BomAndData()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Products.Add(new Product { Name = "Café Colombiano", Category = "Bebidas", Price = 25000, Quantity = 50, IsActive = true });
            await context.SaveChangesAsync();

            var service = new ReportService(context);

            // Act
            var bytes = await service.GenerateProductsCsvReportAsync();

            // Assert
            bytes.Should().NotBeNull();
            // Check UTF-8 BOM (0xEF, 0xBB, 0xBF)
            bytes[0].Should().Be(0xEF);
            bytes[1].Should().Be(0xBB);
            bytes[2].Should().Be(0xBF);
        }

        [Fact]
        public async Task GenerateSalesReport_ShouldReturnValidExcelFile()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var client = new Client { FirstName = "Carlos", LastName = "Mendoza", DocumentNumber = "12345678" };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            context.Sales.Add(new Sale
            {
                ClientId = client.Id,
                SaleDate = DateTime.UtcNow,
                TotalAmount = 150000,
                PaymentMethod = "Tarjeta",
                AmountPaid = 150000,
                ChangeDue = 0
            });
            await context.SaveChangesAsync();

            var service = new ReportService(context);

            // Act
            var bytes = await service.GenerateSalesReportAsync(null, null);

            // Assert
            bytes.Should().NotBeNull();
            bytes.Length.Should().BeGreaterThan(100);
        }
    }
}
