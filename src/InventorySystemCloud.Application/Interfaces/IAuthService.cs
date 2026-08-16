using System;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
        Task<ApiResponse<string>> LogoutAsync(Guid publicId);
    }
}
