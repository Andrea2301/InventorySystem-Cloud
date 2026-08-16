using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Purchases
{
    public class CreatePurchaseDto
    {
        [Required]
        public int SupplierId { get; set; }

        [MaxLength(100)]
        public string? InvoiceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "COP";

        [Required]
        [MinLength(1, ErrorMessage = "La orden de compra debe incluir al menos un producto.")]
        public List<CreatePurchaseItemDto> Items { get; set; } = new List<CreatePurchaseItemDto>();
    }
}
