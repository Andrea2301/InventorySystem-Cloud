using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Users
{
    public class CreateCashierDto
    {
        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        public List<string> Permissions { get; set; } = new();
    }
}
