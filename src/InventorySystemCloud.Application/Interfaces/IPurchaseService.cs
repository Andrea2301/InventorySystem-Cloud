using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Purchases;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IPurchaseService
    {
        Task<ApiResponse<PurchaseResponseDto>> CreatePurchaseAsync(CreatePurchaseDto request, Guid userPublicId);
        Task<ApiResponse<IEnumerable<PurchaseResponseDto>>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null, int? supplierId = null);
        Task<ApiResponse<PurchaseResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<PurchaseReportDto>> GetDailyReportAsync(DateTime? date = null);
    }
}
