using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Infrastructure.Data
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<SaleDetail> SaleDetails { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PublicId)
                .IsUnique();

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.Email)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.AmountPaid)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.ChangeDue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleDetail>()
                .Property(sd => sd.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SaleDetail>()
                .Property(sd => sd.TotalPrice)
                .HasPrecision(18, 2);
        }
    }
}
