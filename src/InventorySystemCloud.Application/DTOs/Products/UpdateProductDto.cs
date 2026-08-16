using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Products
{
    public class UpdateProductDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "General";

        [Required]
        [Range(0.01, 1_000_000, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 100_000, ErrorMessage = "La cantidad no puede ser negativa.")]
        public int Quantity { get; set; }

        [MaxLength(500)]
        public string? ImagePath { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
