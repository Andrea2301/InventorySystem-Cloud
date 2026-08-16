using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(12)]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        public string? CaptchaToken { get; set; }
    }
}
