using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Sales
{
    public class CreateSaleDto
    {
        [Required(ErrorMessage = "Client is required.")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Efectivo";

        [Required(ErrorMessage = "Amount paid is required.")]
        [Range(0.01, 10_000_000, ErrorMessage = "Amount paid must be greater than 0.")]
        public decimal AmountPaid { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "COP";

        [Required(ErrorMessage = "Sale must contain at least one product.")]
        [MinLength(1, ErrorMessage = "Sale must contain at least one product.")]
        public List<CreateSaleItemDto> Items { get; set; } = new();
    }
}
