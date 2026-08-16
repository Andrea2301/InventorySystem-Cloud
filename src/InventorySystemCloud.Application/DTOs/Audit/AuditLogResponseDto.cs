using System;

namespace InventorySystemCloud.Application.DTOs.Audit
{
    public class AuditLogResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
