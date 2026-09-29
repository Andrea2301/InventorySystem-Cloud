using System.Collections.Generic;

namespace InventorySystemCloud.Application.DTOs.Users
{
    public class PermissionGroupDto
    {
        public string Module { get; set; } = string.Empty;
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    public class PermissionItemDto
    {
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
