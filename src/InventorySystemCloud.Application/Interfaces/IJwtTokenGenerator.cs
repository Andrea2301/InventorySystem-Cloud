using InventorySystemCloud.Domain.Entities;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
