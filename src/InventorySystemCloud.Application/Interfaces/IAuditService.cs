using System.Collections.Generic;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Audit;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogActionAsync(int userId, string action, string? details = null);
        Task<ApiResponse<IEnumerable<AuditLogResponseDto>>> GetRecentLogsAsync(int count = 50);
    }
}
