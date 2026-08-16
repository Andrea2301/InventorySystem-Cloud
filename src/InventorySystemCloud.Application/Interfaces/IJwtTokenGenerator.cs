using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Domain.Entities;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        GeneratedToken GenerateToken(User user);
    }
}
