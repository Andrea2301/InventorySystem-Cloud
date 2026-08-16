using System.Collections.Generic;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Suppliers;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface ISupplierService
    {
        Task<ApiResponse<IEnumerable<SupplierResponseDto>>> GetAllAsync(bool includeInactive = false, string? searchTerm = null);
        Task<ApiResponse<SupplierResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<SupplierResponseDto>> CreateAsync(CreateSupplierDto request);
        Task<ApiResponse<SupplierResponseDto>> UpdateAsync(int id, UpdateSupplierDto request);
        Task<ApiResponse<string>> DeleteAsync(int id);
    }
}
