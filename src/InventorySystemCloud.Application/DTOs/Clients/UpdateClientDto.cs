using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Clients
{
    public class UpdateClientDto
    {
        [Required(ErrorMessage = "The document number is required.")]
        [MaxLength(20)]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "The first name is required.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "The last name is required.")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "The email address is required.")]
        [EmailAddress(ErrorMessage = "The email format is not valid.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "The phone number format is not valid.")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
