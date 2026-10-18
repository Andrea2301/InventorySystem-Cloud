using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Domain.Constants;
using InventorySystemCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventorySystemCloud.Infrastructure.Data
{
    public static class DemoDataSeeder
    {
        private const int PasswordWorkFactor = 12;

        public static async Task SeedAllDemoDataAsync(AppDbContext context, ILogger logger)
        {
            try
            {
                // 1. Seed / Ensure Demo Admin & Demo Cashier
                var adminUser = await EnsureDemoAdminAsync(context, logger);
                var cashierUser = await EnsureDemoCashierAsync(context, logger);

                // 2. Check if products already exist to avoid duplicate seeding
                if (await context.Products.AnyAsync())
                {
                    logger.LogInformation("Demo catalog products already exist. Skipping catalog seeding.");
                    return;
                }

                logger.LogInformation("Starting rich demo data seeding (Products, Clients, Suppliers, Sales, Purchases)...");

                // 3. Seed Products with realistic categories and image URLs
                var products = GetDemoProducts();
                context.Products.AddRange(products);
                await context.SaveChangesAsync();

                // 4. Seed Clients
                var clients = GetDemoClients();
                context.Clients.AddRange(clients);
                await context.SaveChangesAsync();

                // 5. Seed Suppliers
                var suppliers = GetDemoSuppliers();
                context.Suppliers.AddRange(suppliers);
                await context.SaveChangesAsync();

                // 6. Seed Historical Sales (with details)
                var sales = GetDemoSales(clients, products, cashierUser.Id);
                context.Sales.AddRange(sales);

                // 7. Seed Historical Purchases (with details)
                var purchases = GetDemoPurchases(suppliers, products, adminUser.Id);
                context.Purchases.AddRange(purchases);

                // 8. Seed Initial Audit Logs
                var auditLogs = new List<AuditLog>
                {
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        Action = "DEMO_INITIALIZATION",
                        Details = "Inicialización y sembrado del entorno de demostración de InventorySystem Cloud.",
                        Timestamp = DateTime.UtcNow.AddDays(-7)
                    },
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        Action = "CATALOG_IMPORT",
                        Details = "Carga inicial de 12 productos tecnológicos y mobiliario en el catálogo demo.",
                        Timestamp = DateTime.UtcNow.AddDays(-6)
                    },
                    new AuditLog
                    {
                        UserId = cashierUser.Id,
                        Action = "POS_SESSION_START",
                        Details = "Apertura de turno en punto de venta (POS) cajero demo.",
                        Timestamp = DateTime.UtcNow.AddDays(-1)
                    }
                };
                context.AuditLogs.AddRange(auditLogs);

                await context.SaveChangesAsync();
                logger.LogInformation("Rich demo data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding demo data: {Message}", ex.Message);
            }
        }

        public static async Task ResetDemoDataAsync(AppDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Resetting demo database to initial clean state...");

                // Clear child tables first
                context.SaleDetails.RemoveRange(await context.SaleDetails.ToListAsync());
                context.PurchaseDetails.RemoveRange(await context.PurchaseDetails.ToListAsync());
                context.Sales.RemoveRange(await context.Sales.ToListAsync());
                context.Purchases.RemoveRange(await context.Purchases.ToListAsync());
                context.AuditLogs.RemoveRange(await context.AuditLogs.ToListAsync());
                context.Products.RemoveRange(await context.Products.ToListAsync());
                context.Clients.RemoveRange(await context.Clients.ToListAsync());
                context.Suppliers.RemoveRange(await context.Suppliers.ToListAsync());

                // Reset non-primary users
                var secondaryUsers = await context.Users
                    .Where(u => u.Email != "admin@demo.com" && u.Email != "cajero@demo.com")
                    .ToListAsync();
                context.Users.RemoveRange(secondaryUsers);

                await context.SaveChangesAsync();

                // Re-seed everything
                await SeedAllDemoDataAsync(context, logger);
                logger.LogInformation("Demo database reset completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while resetting demo data: {Message}", ex.Message);
                throw;
            }
        }

        private static async Task<User> EnsureDemoAdminAsync(AppDbContext context, ILogger logger)
        {
            var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@demo.com");
            if (admin == null)
            {
                admin = new User
                {
                    Email = "admin@demo.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminDemo123*!", workFactor: PasswordWorkFactor),
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N")
                };
                context.Users.Add(admin);
                await context.SaveChangesAsync();
                logger.LogInformation("Demo Admin seeded: admin@demo.com");
            }
            else
            {
                admin.IsActive = true;
                admin.Role = UserRole.Admin;
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminDemo123*!", workFactor: PasswordWorkFactor);
                await context.SaveChangesAsync();
            }
            return admin;
        }

        private static async Task<User> EnsureDemoCashierAsync(AppDbContext context, ILogger logger)
        {
            var cashier = await context.Users
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(u => u.Email == "cajero@demo.com");

            var defaultPermissions = new[]
            {
                AppPermissions.Sales.View,
                AppPermissions.Sales.Create,
                AppPermissions.Sales.Invoice,
                AppPermissions.Products.View,
                AppPermissions.Clients.View,
                AppPermissions.Clients.Create,
                AppPermissions.Reports.Sales
            };

            if (cashier == null)
            {
                cashier = new User
                {
                    Email = "cajero@demo.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("CashierDemo123*!", workFactor: PasswordWorkFactor),
                    Role = UserRole.Cashier,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N")
                };

                foreach (var perm in defaultPermissions)
                {
                    cashier.Permissions.Add(new UserPermission
                    {
                        Permission = perm,
                        User = cashier
                    });
                }

                context.Users.Add(cashier);
                await context.SaveChangesAsync();
                logger.LogInformation("Demo Cashier seeded: cajero@demo.com");
            }
            else
            {
                cashier.IsActive = true;
                cashier.PasswordHash = BCrypt.Net.BCrypt.HashPassword("CashierDemo123*!", workFactor: PasswordWorkFactor);

                foreach (var perm in defaultPermissions)
                {
                    if (!cashier.Permissions.Any(p => p.Permission == perm))
                    {
                        cashier.Permissions.Add(new UserPermission { Permission = perm, User = cashier });
                    }
                }
                await context.SaveChangesAsync();
            }

            return cashier;
        }

        private static List<Product> GetDemoProducts()
        {
            return new List<Product>
            {
                new Product
                {
                    Name = "Laptop Dell XPS 15 9530",
                    Description = "Pantalla OLED 3.5K, Intel Core i7 13va Gen, 32GB RAM, 1TB SSD NVMe, NVIDIA RTX 4060.",
                    Category = "Computadores",
                    Price = 1850.00m,
                    Quantity = 14,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Product
                {
                    Name = "Apple MacBook Pro 14\" M3",
                    Description = "Chip Apple M3 Pro, CPU 11 núcleos, GPU 14 núcleos, 18GB memoria unificada, 512GB SSD.",
                    Category = "Computadores",
                    Price = 1999.00m,
                    Quantity = 8,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-28)
                },
                new Product
                {
                    Name = "Monitor Dell UltraSharp 27\" 4K USB-C Hub",
                    Description = "Resolución 3840x2160 IPS Black, 98% DCI-P3, puertos USB-C con suministro de 90W.",
                    Category = "Monitores",
                    Price = 520.00m,
                    Quantity = 22,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                },
                new Product
                {
                    Name = "Mouse Inalámbrico Logitech MX Master 3S",
                    Description = "Sensor 8K DPI silencioso, rueda MagSpeed electromagnética, conectividad Bluetooth y Bolt.",
                    Category = "Periféricos",
                    Price = 119.00m,
                    Quantity = 45,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new Product
                {
                    Name = "Teclado Mecánico Inalámbrico Keychron K2 Pro",
                    Description = "Distribución compacta 75%, switches mecánicos Gateron G Pro Red, retroiluminación RGB.",
                    Category = "Periféricos",
                    Price = 99.00m,
                    Quantity = 30,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-18)
                },
                new Product
                {
                    Name = "Auriculares Sony WH-1000XM5 con Noise Cancelling",
                    Description = "Cancelación de ruido líder en la industria con procesador V1, batería hasta 30 horas.",
                    Category = "Audio",
                    Price = 379.00m,
                    Quantity = 16,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new Product
                {
                    Name = "Silla Ergonómica Herman Miller Aeron Remastered",
                    Description = "Soporte PostureFit SL ajustable, malla Pellicle 8Z transpirable, reclinación armónica.",
                    Category = "Oficina",
                    Price = 1290.00m,
                    Quantity = 6,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1580481077114-1e23118cf19c?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                },
                new Product
                {
                    Name = "Escritorio Elevable Eléctrico SmartDesk Pro",
                    Description = "Doble motor silencioso, altura ajustable 65cm a 130cm, tablero de roble macizo 160x80cm.",
                    Category = "Oficina",
                    Price = 590.00m,
                    Quantity = 10,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1518455027359-f3f8164ba6bd?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Product
                {
                    Name = "Hub USB-C 8 en 1 Anker PowerExpand",
                    Description = "Salida HDMI 4K@60Hz, suministro de energía Power Delivery 100W, Gigabit Ethernet, lector SD.",
                    Category = "Accesorios",
                    Price = 59.99m,
                    Quantity = 50,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1622445262464-84b1456045b6?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Product
                {
                    Name = "Cargador Rápido GaN 100W Baseus 4 Puertos",
                    Description = "Tecnología GaN III, 2x USB-C y 2x USB-A, compatible con laptops, smartphones y tablets.",
                    Category = "Accesorios",
                    Price = 49.50m,
                    Quantity = 65,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1583863788434-e58a36330cf0?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new Product
                {
                    Name = "Apple iPad Air 11\" M2 128GB Wi-Fi",
                    Description = "Pantalla Liquid Retina, compatible con Apple Pencil Pro y Magic Keyboard, cámara frontal horizontal.",
                    Category = "Tablets",
                    Price = 599.00m,
                    Quantity = 11,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Product
                {
                    Name = "Disco SSD Portátil Samsung T7 Shield 1TB",
                    Description = "Velocidad de transferencia hasta 1050 MB/s, resistencia al agua y polvo IP65, diseño rugerizado.",
                    Category = "Almacenamiento",
                    Price = 129.99m,
                    Quantity = 0, // Out of stock to test zero-stock validation
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1597872200969-2b65d56bd16b?w=600&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };
        }

        private static List<Client> GetDemoClients()
        {
            return new List<Client>
            {
                new Client
                {
                    FirstName = "TechSolutions",
                    LastName = "Latam S.A.S",
                    DocumentNumber = "901234567-1",
                    Email = "contacto@techsolutions.com",
                    PhoneNumber = "+57 310 555 1234",
                    Address = "Cra 15 # 93-60, Oficina 402, Bogotá",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                },
                new Client
                {
                    FirstName = "Carlos Alberto",
                    LastName = "Mendoza Díaz",
                    DocumentNumber = "1098765432",
                    Email = "carlos.mendoza@email.com",
                    PhoneNumber = "+57 300 444 5678",
                    Address = "Calle 72 # 11-28, Apto 501, Bogotá",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new Client
                {
                    FirstName = "Inversiones Andinas",
                    LastName = "del Valle Ltda.",
                    DocumentNumber = "800987654-3",
                    Email = "compras@inversionesandinas.com",
                    PhoneNumber = "+57 315 222 9988",
                    Address = "Av 6 Norte # 24N-10, Cali",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-18)
                },
                new Client
                {
                    FirstName = "Mariana",
                    LastName = "Restrepo Gómez",
                    DocumentNumber = "1023456789",
                    Email = "mariana.restrepo@email.com",
                    PhoneNumber = "+57 312 888 7766",
                    Address = "Transversal 39 # 74-15, Medellín",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new Client
                {
                    FirstName = "Distribuciones",
                    LastName = "Globales S.A.",
                    DocumentNumber = "900112233-4",
                    Email = "info@distribucionesglobales.com",
                    PhoneNumber = "+57 320 111 4455",
                    Address = "Cl 100 # 19A-40, Piso 8, Bogotá",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };
        }

        private static List<Supplier> GetDemoSuppliers()
        {
            return new List<Supplier>
            {
                new Supplier
                {
                    CompanyName = "Mayorista Tecnológico de Colombia",
                    Email = "ventas@mayoristatech.com",
                    PhoneNumber = "+57 601 744 1234",
                    Website = "https://www.mayoristatech.com",
                    Category = "Computadores y Periféricos",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Supplier
                {
                    CompanyName = "Conectividad & Redes Globales S.A.",
                    Email = "contacto@redesglobales.com",
                    PhoneNumber = "+57 601 888 5678",
                    Website = "https://www.redesglobales.com",
                    Category = "Accesorios y Conectividad",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                },
                new Supplier
                {
                    CompanyName = "Mobiliario Corporativo ErgoComfort",
                    Email = "info@ergocomfort.com",
                    PhoneNumber = "+57 601 333 9900",
                    Website = "https://www.ergocomfort.com",
                    Category = "Mobiliario y Ergonomía",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-22)
                },
                new Supplier
                {
                    CompanyName = "Audio & Gadgets Internacional",
                    Email = "soporte@audiogadgets.com",
                    PhoneNumber = "+57 601 222 1144",
                    Website = "https://www.audiogadgets.com",
                    Category = "Audio y Gadgets",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                }
            };
        }

        private static List<Sale> GetDemoSales(List<Client> clients, List<Product> products, int cashierUserId)
        {
            var p1 = products.First(p => p.Name.Contains("Dell XPS 15"));
            var p2 = products.First(p => p.Name.Contains("MacBook Pro"));
            var p3 = products.First(p => p.Name.Contains("Monitor Dell"));
            var p4 = products.First(p => p.Name.Contains("Logitech MX Master"));
            var p5 = products.First(p => p.Name.Contains("Keychron K2"));
            var p6 = products.First(p => p.Name.Contains("Sony WH-1000XM5"));
            var p7 = products.First(p => p.Name.Contains("Hub USB-C"));

            var sales = new List<Sale>();

            // Venta 1 (hace 5 días)
            var s1 = new Sale
            {
                ClientId = clients[0].Id,
                CreatedByUserId = cashierUserId,
                SaleDate = DateTime.UtcNow.AddDays(-5).AddHours(-2),
                PaymentMethod = "Transferencia",
                Currency = "USD",
                TotalAmount = 2489.00m,
                AmountPaid = 2489.00m,
                ChangeDue = 0.00m,
                SaleDetails = new List<SaleDetail>
                {
                    new SaleDetail { ProductId = p1.Id, Quantity = 1, UnitPrice = p1.Price, TotalPrice = p1.Price },
                    new SaleDetail { ProductId = p3.Id, Quantity = 1, UnitPrice = p3.Price, TotalPrice = p3.Price },
                    new SaleDetail { ProductId = p4.Id, Quantity = 1, UnitPrice = p4.Price, TotalPrice = p4.Price }
                }
            };
            sales.Add(s1);

            // Venta 2 (hace 4 días)
            var s2 = new Sale
            {
                ClientId = clients[1].Id,
                CreatedByUserId = cashierUserId,
                SaleDate = DateTime.UtcNow.AddDays(-4).AddHours(-4),
                PaymentMethod = "Tarjeta Crédito",
                Currency = "USD",
                TotalAmount = 218.00m,
                AmountPaid = 218.00m,
                ChangeDue = 0.00m,
                SaleDetails = new List<SaleDetail>
                {
                    new SaleDetail { ProductId = p4.Id, Quantity = 1, UnitPrice = p4.Price, TotalPrice = p4.Price },
                    new SaleDetail { ProductId = p5.Id, Quantity = 1, UnitPrice = p5.Price, TotalPrice = p5.Price }
                }
            };
            sales.Add(s2);

            // Venta 3 (hace 3 días)
            var s3 = new Sale
            {
                ClientId = clients[2].Id,
                CreatedByUserId = cashierUserId,
                SaleDate = DateTime.UtcNow.AddDays(-3).AddHours(-1),
                PaymentMethod = "Transferencia",
                Currency = "USD",
                TotalAmount = 2378.00m,
                AmountPaid = 2378.00m,
                ChangeDue = 0.00m,
                SaleDetails = new List<SaleDetail>
                {
                    new SaleDetail { ProductId = p2.Id, Quantity = 1, UnitPrice = p2.Price, TotalPrice = p2.Price },
                    new SaleDetail { ProductId = p6.Id, Quantity = 1, UnitPrice = p6.Price, TotalPrice = p6.Price }
                }
            };
            sales.Add(s3);

            // Venta 4 (hace 2 días)
            var s4 = new Sale
            {
                ClientId = clients[3].Id,
                CreatedByUserId = cashierUserId,
                SaleDate = DateTime.UtcNow.AddDays(-2).AddHours(-3),
                PaymentMethod = "Efectivo",
                Currency = "USD",
                TotalAmount = 438.99m,
                AmountPaid = 450.00m,
                ChangeDue = 11.01m,
                SaleDetails = new List<SaleDetail>
                {
                    new SaleDetail { ProductId = p6.Id, Quantity = 1, UnitPrice = p6.Price, TotalPrice = p6.Price },
                    new SaleDetail { ProductId = p7.Id, Quantity = 1, UnitPrice = p7.Price, TotalPrice = p7.Price }
                }
            };
            sales.Add(s4);

            // Venta 5 (Ayer)
            var s5 = new Sale
            {
                ClientId = clients[4].Id,
                CreatedByUserId = cashierUserId,
                SaleDate = DateTime.UtcNow.AddDays(-1).AddHours(-5),
                PaymentMethod = "Tarjeta Débito",
                Currency = "USD",
                TotalAmount = 1040.00m,
                AmountPaid = 1040.00m,
                ChangeDue = 0.00m,
                SaleDetails = new List<SaleDetail>
                {
                    new SaleDetail { ProductId = p3.Id, Quantity = 2, UnitPrice = p3.Price, TotalPrice = p3.Price * 2 }
                }
            };
            sales.Add(s5);

            // Venta 6 (Hoy)
            var s6 = new Sale
            {
                ClientId = clients[0].Id,
                CreatedByUserId = cashierUserId,
                SaleDate = DateTime.UtcNow.AddHours(-1),
                PaymentMethod = "Efectivo",
                Currency = "USD",
                TotalAmount = 119.00m,
                AmountPaid = 120.00m,
                ChangeDue = 1.00m,
                SaleDetails = new List<SaleDetail>
                {
                    new SaleDetail { ProductId = p4.Id, Quantity = 1, UnitPrice = p4.Price, TotalPrice = p4.Price }
                }
            };
            sales.Add(s6);

            return sales;
        }

        private static List<Purchase> GetDemoPurchases(List<Supplier> suppliers, List<Product> products, int adminUserId)
        {
            var p1 = products.First(p => p.Name.Contains("Dell XPS 15"));
            var p3 = products.First(p => p.Name.Contains("Monitor Dell"));
            var p4 = products.First(p => p.Name.Contains("Logitech MX Master"));

            return new List<Purchase>
            {
                new Purchase
                {
                    SupplierId = suppliers[0].Id,
                    CreatedByUserId = adminUserId,
                    InvoiceNumber = "FAC-TECH-2026-001",
                    PurchaseDate = DateTime.UtcNow.AddDays(-15),
                    Currency = "USD",
                    Notes = "Lote inicial de computadores portátiles y monitores de alta resolución.",
                    TotalAmount = 14500.00m,
                    PurchaseDetails = new List<PurchaseDetail>
                    {
                        new PurchaseDetail { ProductId = p1.Id, Quantity = 10, UnitPrice = 1300.00m, TotalPrice = 13000.00m },
                        new PurchaseDetail { ProductId = p3.Id, Quantity = 5, UnitPrice = 300.00m, TotalPrice = 1500.00m }
                    }
                },
                new Purchase
                {
                    SupplierId = suppliers[1].Id,
                    CreatedByUserId = adminUserId,
                    InvoiceNumber = "FAC-ACC-2026-089",
                    PurchaseDate = DateTime.UtcNow.AddDays(-8),
                    Currency = "USD",
                    Notes = "Reabastecimiento de periféricos ergonómicos inalámbricos.",
                    TotalAmount = 2400.00m,
                    PurchaseDetails = new List<PurchaseDetail>
                    {
                        new PurchaseDetail { ProductId = p4.Id, Quantity = 30, UnitPrice = 80.00m, TotalPrice = 2400.00m }
                    }
                }
            };
        }
    }
}
