using System;
using System.Collections.Generic;
using FluentAssertions;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Infrastructure.Services;
using Xunit;

namespace InventorySystemCloud.UnitTests.Services
{
    public class EmailGeneratorTests
    {
        private readonly EmailGenerator _generator = new();

        [Fact]
        public void GenerateWelcomeEmail_ContainsUserNameAndAppBranding()
        {
            // Act
            var html = _generator.GenerateWelcomeEmail("Camila", "camila@example.com");

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("Camila");
            html.Should().Contain("camila@example.com");
            html.Should().Contain("InventorySystem Cloud");
            html.Should().Contain("Bienvenido");
        }

        [Fact]
        public void GenerateInvoiceEmail_ContainsSaleDetailsAndInvoiceNumber()
        {
            // Arrange
            var sale = new SaleResponseDto
            {
                Id = 42,
                ClientName = "Juan Perez",
                SaleDate = new DateTime(2026, 8, 16, 15, 30, 0),
                PaymentMethod = "Efectivo",
                TotalAmount = 150000m,
                Currency = "COP",
                Items = new List<SaleDetailResponseDto>
                {
                    new() { ProductName = "Café Especial 500g", Quantity = 3, UnitPrice = 50000m, TotalPrice = 150000m }
                }
            };

            // Act
            var html = _generator.GenerateInvoiceEmail(sale);

            // Assert
            html.Should().NotBeNullOrWhiteSpace();
            html.Should().Contain("Juan Perez");
            html.Should().Contain("FAC-000042");
            html.Should().Contain("Efectivo");
            html.Should().Contain("150");
        }
    }
}
