using System;
using System.Threading.Tasks;
using InventorySystemCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventorySystemCloud.Infrastructure.Data
{
    public static class DataSeeder
    {
        private const int PasswordWorkFactor = 12;

        public static async Task SeedInitialAdminAsync(
            AppDbContext context,
            IConfiguration configuration,
            ILogger logger)
        {
            try
            {
                // Check if any Admin user already exists (Idempotent check)
                var adminExists = await context.Users.AnyAsync(u => u.Role == UserRole.Admin);
                if (adminExists)
                {
                    logger.LogInformation("Administrator user already exists in the database. Seeding skipped.");
                    return;
                }

                var adminEmail = configuration["InitialAdmin:Email"]?.Trim().ToLowerInvariant();
                var adminPassword = configuration["InitialAdmin:Password"];

                if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                {
                    logger.LogWarning(
                        "Initial administrator not seeded: 'InitialAdmin:Email' or 'InitialAdmin:Password' are not configured. " +
                        "Configure them in environment variables or configuration files to create the initial admin.");
                    return;
                }

                var existingByEmail = await context.Users.AnyAsync(u => u.Email == adminEmail);
                if (existingByEmail)
                {
                    logger.LogWarning("Cannot seed initial admin: user with email '{Email}' already exists with a different role.", adminEmail);
                    return;
                }

                var adminUser = new User
                {
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: PasswordWorkFactor),
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N")
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();

                logger.LogInformation("Initial administrator account successfully seeded for '{Email}'.", adminEmail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the initial administrator account.");
            }
        }
    }
}
