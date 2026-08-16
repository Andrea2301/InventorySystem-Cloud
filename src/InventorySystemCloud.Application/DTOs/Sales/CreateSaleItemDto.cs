using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Application.DTOs.Sales
{
    public class CreateSaleItemDto
    {
        [Required(ErrorMessage = "El identificador del producto es obligatorio.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, 100_000, ErrorMessage = "La cantidad debe ser al menos 1.")]
        public int Quantity { get; set; }
    }
}
