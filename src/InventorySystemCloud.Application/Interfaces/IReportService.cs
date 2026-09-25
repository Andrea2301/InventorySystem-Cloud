using System;
using System.Threading.Tasks;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IReportService
    {
        Task<byte[]> GenerateProductsExcelReportAsync();
        Task<byte[]> GenerateProductsCsvReportAsync();
        Task<byte[]> GenerateSalesReportAsync(DateTime? startDate, DateTime? endDate);
        Task<byte[]> GeneratePurchasesReportAsync(DateTime? startDate, DateTime? endDate);
        Task<byte[]> GenerateAuditLogsReportAsync(DateTime? startDate, DateTime? endDate);
    }
}
