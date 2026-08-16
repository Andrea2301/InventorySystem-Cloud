using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Clients
{
    public class UpdateClientDto
    {
        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [MaxLength(20)]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato de correo no es válido.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "El formato de teléfono no es válido.")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
