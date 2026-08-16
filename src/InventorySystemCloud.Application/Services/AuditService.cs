using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Audit;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Domain.Entities;
using InventorySystemCloud.Shared;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAppDbContext _context;

        public AuditService(IAppDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(int userId, string action, string? details = null)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Audit logging should not crash the main business operation
            }
        }

        public async Task<ApiResponse<IEnumerable<AuditLogResponseDto>>> GetRecentLogsAsync(int count = 50)
        {
            var logs = await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Take(Math.Clamp(count, 1, 200))
                .Select(a => new AuditLogResponseDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserEmail = a.User != null ? a.User.Email : "Unknown",
                    Action = a.Action,
                    Details = a.Details,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();

            return ApiResponse<IEnumerable<AuditLogResponseDto>>.SuccessResponse(logs);
        }
    }
}
