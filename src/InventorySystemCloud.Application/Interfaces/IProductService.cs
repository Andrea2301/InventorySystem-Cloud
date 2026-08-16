using System.Collections.Generic;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Products;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IProductService
    {
        Task<ApiResponse<IEnumerable<ProductResponseDto>>> GetAllAsync(bool includeInactive = false);
        Task<ApiResponse<ProductResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<ProductResponseDto>> CreateAsync(CreateProductDto request);
        Task<ApiResponse<ProductResponseDto>> UpdateAsync(int id, UpdateProductDto request);
        Task<ApiResponse<string>> DeleteAsync(int id);
    }
}
