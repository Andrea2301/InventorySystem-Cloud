using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventorySystemCloud.Domain.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "General";

        [Required]
        [Range(0, 1000000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 100000)]
        public int Quantity { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public byte[]? ImageData { get; set; }

        public string? ImagePath { get; set; }

        [NotMapped]
        public string? ImageSourceUri => string.IsNullOrEmpty(ImagePath)
            ? null
            : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImagePath);

        [NotMapped]
        public string Status => IsActive ? (Quantity > 0 ? "In Stock" : "Out of Stock") : "Inactive";
    }
}
