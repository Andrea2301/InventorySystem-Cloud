using System;

namespace InventorySystemCloud.Application.DTOs.Auth
{
    public sealed class GeneratedToken
    {
        public string Value { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }
}
