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

        // =========================================================================================
        // [DEPLOYMENT - SEGURIDAD]: SEMBRADO DE CAJERO DEMO
        // Para producción real: No ejecutes este sembrado o define valores seguros mediante
        // variables de entorno:
        // - DemoCashier__Email
        // - DemoCashier__Password
        // =========================================================================================
        public static async Task SeedDemoCashierAsync(AppDbContext context, IConfiguration configuration, ILogger logger)
        {
            try
            {
                var cashierEmail = configuration["DemoCashier:Email"]?.Trim().ToLowerInvariant() ?? "cajero@inventorycloud.com";
                var cashierPassword = configuration["DemoCashier:Password"] ?? "CashierDemo123*!";

                var cashierExists = await context.Users.AnyAsync(u => u.Email == cashierEmail);
                if (cashierExists)
                {
                    logger.LogInformation("Demo cashier account already exists. Seeding skipped.");
                    return;
                }

                var cashierUser = new User
                {
                    Email = cashierEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(cashierPassword, workFactor: PasswordWorkFactor),
                    Role = UserRole.Cashier,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N")
                };

                // Assign default demo permissions: Point of Sale & Product/Client catalog consultation
                var demoPermissions = new[]
                {
                    Domain.Constants.AppPermissions.Sales.View,
                    Domain.Constants.AppPermissions.Sales.Create,
                    Domain.Constants.AppPermissions.Sales.Invoice,
                    Domain.Constants.AppPermissions.Products.View,
                    Domain.Constants.AppPermissions.Clients.View,
                    Domain.Constants.AppPermissions.Clients.Create
                };

                foreach (var perm in demoPermissions)
                {
                    cashierUser.Permissions.Add(new UserPermission
                    {
                        Permission = perm,
                        User = cashierUser
                    });
                }

                context.Users.Add(cashierUser);
                await context.SaveChangesAsync();

                logger.LogInformation("Demo cashier account successfully seeded for '{Email}'.", cashierEmail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the demo cashier account.");
            }
        }
    }
}
