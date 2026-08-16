using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Sales
{
    public class CreateSaleDto
    {
        [Required(ErrorMessage = "El cliente es obligatorio.")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "El método de pago es obligatorio.")]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Efectivo";

        [Required(ErrorMessage = "El monto pagado es obligatorio.")]
        [Range(0.01, 10_000_000, ErrorMessage = "El monto pagado debe ser mayor a 0.")]
        public decimal AmountPaid { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "COP";

        [Required(ErrorMessage = "La venta debe contener al menos un producto.")]
        [MinLength(1, ErrorMessage = "La venta debe contener al menos un producto.")]
        public List<CreateSaleItemDto> Items { get; set; } = new();
    }
}
