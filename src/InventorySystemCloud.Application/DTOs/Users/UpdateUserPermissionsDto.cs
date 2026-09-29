using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Users
{
    public class UpdateUserPermissionsDto
    {
        [Required(ErrorMessage = "La lista de permisos es requerida.")]
        public List<string> Permissions { get; set; } = new();
    }
}
