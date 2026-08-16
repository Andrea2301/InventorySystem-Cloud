using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InventorySystemCloud.Application.DTOs.Clients;
using InventorySystemCloud.Application.Services;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventorySystemCloud.UnitTests.Services
{
    public class ClientServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsActiveClients_ByDefault()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Clients.AddRange(
                new Client { DocumentNumber = "123", FirstName = "Ana", LastName = "Gomez", Email = "ana@test.com", PhoneNumber = "111", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Client { DocumentNumber = "456", FirstName = "Carlos", LastName = "Ruiz", Email = "carlos@test.com", PhoneNumber = "222", IsActive = false, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            // Act
            var result = await service.GetAllAsync(includeInactive: false);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data!.First().DocumentNumber.Should().Be("123");
        }

        [Fact]
        public async Task GetAllAsync_WithSearchTerm_FiltersCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Clients.AddRange(
                new Client { DocumentNumber = "123", FirstName = "Ana", LastName = "Gomez", Email = "ana@test.com", PhoneNumber = "111", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Client { DocumentNumber = "456", FirstName = "Bernardo", LastName = "Lopez", Email = "blopez@test.com", PhoneNumber = "222", IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            // Act
            var result = await service.GetAllAsync(searchTerm: "lopez");

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data!.First().LastName.Should().Be("Lopez");
        }

        [Fact]
        public async Task GetByDocumentNumberAsync_WithExistingDoc_ReturnsClient()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var client = new Client { DocumentNumber = "DOC-999", FirstName = "Maria", LastName = "Perez", Email = "maria@test.com", PhoneNumber = "555", IsActive = true, CreatedAt = DateTime.UtcNow };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            // Act
            var result = await service.GetByDocumentNumberAsync("DOC-999");

            // Assert
            result.Success.Should().BeTrue();
            result.Data!.FirstName.Should().Be("Maria");
            result.Data.FullName.Should().Be("Maria Perez");
        }

        [Fact]
        public async Task CreateAsync_WithValidData_CreatesClient()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new ClientService(context);

            var request = new CreateClientDto
            {
                DocumentNumber = "778899",
                FirstName = "Juan",
                LastName = "Diaz",
                Email = "juan.diaz@test.com",
                PhoneNumber = "99887766"
            };

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusCode.Should().Be(201);
            result.Data!.DocumentNumber.Should().Be("778899");

            var inDb = await context.Clients.FirstOrDefaultAsync(c => c.DocumentNumber == "778899");
            inDb.Should().NotBeNull();
            inDb!.Email.Should().Be("juan.diaz@test.com");
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateDocumentNumber_ReturnsConflict()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.Clients.Add(new Client { DocumentNumber = "DUP-01", FirstName = "A", LastName = "B", Email = "a@test.com", PhoneNumber = "1", IsActive = true, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new ClientService(context);
            var request = new CreateClientDto { DocumentNumber = "DUP-01", FirstName = "C", LastName = "D", Email = "c@test.com", PhoneNumber = "2" };

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(409);
        }

        [Fact]
        public async Task DeleteAsync_WithExistingId_SoftDeletesClient()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var client = new Client { DocumentNumber = "DEL-01", FirstName = "Test", LastName = "User", Email = "del@test.com", PhoneNumber = "000", IsActive = true, CreatedAt = DateTime.UtcNow };
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            // Act
            var result = await service.DeleteAsync(client.Id);

            // Assert
            result.Success.Should().BeTrue();

            var inDb = await context.Clients.FindAsync(client.Id);
            inDb!.IsActive.Should().BeFalse();
        }
    }
}
