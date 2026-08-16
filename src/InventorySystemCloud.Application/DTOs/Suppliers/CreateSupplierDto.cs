using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Suppliers
{
    public class CreateSupplierDto
    {
        [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
        [MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato de correo no es válido.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "El formato de teléfono no es válido.")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Website { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }
    }
}
