using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface ISaleService
    {
        Task<ApiResponse<SaleResponseDto>> CreateSaleAsync(CreateSaleDto request, Guid userPublicId);
        Task<ApiResponse<IEnumerable<SaleResponseDto>>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null, int? clientId = null);
        Task<ApiResponse<SaleResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<SaleReportDto>> GetDailyReportAsync(DateTime? date = null);
        Task<ApiResponse<byte[]>> GetInvoicePdfAsync(int saleId);
        Task<ApiResponse<string>> SendInvoiceEmailAsync(int saleId);
    }
}
