using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Sales
{
    public class CreateSaleItemDto
    {
        [Required(ErrorMessage = "Product ID is required.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 100_000, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }
}
