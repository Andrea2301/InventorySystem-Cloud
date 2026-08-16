using System.Collections.Generic;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Clients;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IClientService
    {
        Task<ApiResponse<IEnumerable<ClientResponseDto>>> GetAllAsync(bool includeInactive = false, string? searchTerm = null);
        Task<ApiResponse<ClientResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<ClientResponseDto>> GetByDocumentNumberAsync(string documentNumber);
        Task<ApiResponse<ClientResponseDto>> CreateAsync(CreateClientDto request);
        Task<ApiResponse<ClientResponseDto>> UpdateAsync(int id, UpdateClientDto request);
        Task<ApiResponse<string>> DeleteAsync(int id);
    }
}
