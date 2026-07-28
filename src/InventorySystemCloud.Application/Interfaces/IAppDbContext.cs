using System.Threading;
using System.Threading.Tasks;
using InventorySystemCloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Product> Products { get; }
        DbSet<Sale> Sales { get; }
        DbSet<SaleDetail> SaleDetails { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<Client> Clients { get; }
        DbSet<Supplier> Suppliers { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
