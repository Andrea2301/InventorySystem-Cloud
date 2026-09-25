using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Suppliers
{
    public class UpdateSupplierDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Website { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
