using System;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Auth;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, string? ipAddress = null);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request, string? ipAddress = null);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress = null);
        Task<ApiResponse<string>> RevokeTokenAsync(RevokeTokenRequestDto request, string? ipAddress = null);
        Task<ApiResponse<string>> LogoutAsync(Guid publicId);
        Task<ApiResponse<string>> UploadAvatarAsync(Guid publicId, System.IO.Stream fileStream, string fileName);
        Task<ApiResponse<string>> DeleteAvatarAsync(Guid publicId);
    }
}
