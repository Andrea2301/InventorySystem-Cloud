using System.Collections.Generic;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Users;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<List<UserResponseDto>>> GetAllAsync();
        Task<ApiResponse<UserResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<UserResponseDto>> CreateCashierAsync(CreateCashierDto request);
        Task<ApiResponse<UserResponseDto>> UpdatePermissionsAsync(int id, UpdateUserPermissionsDto request);
        Task<ApiResponse<UserResponseDto>> ToggleStatusAsync(int id, bool isActive);
        ApiResponse<List<PermissionGroupDto>> GetPermissionsCatalog();
    }
}
