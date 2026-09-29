using System;
using System.Collections.Generic;

namespace InventorySystemCloud.Application.DTOs.Users
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? AvatarUrl { get; set; }
        public List<string> Permissions { get; set; } = new();
    }
}
