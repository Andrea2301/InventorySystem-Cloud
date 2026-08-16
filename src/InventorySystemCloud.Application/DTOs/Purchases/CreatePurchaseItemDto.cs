using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Purchases
{
    public class CreatePurchaseItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 100000, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, 1000000.00, ErrorMessage = "El precio unitario debe ser mayor a 0.")]
        public decimal UnitPrice { get; set; }
    }
}
