using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Auth
{
    public class RevokeTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
