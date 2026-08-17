using InventorySystemCloud.Application.DTOs.Sales;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IPdfInvoiceGenerator
    {
        byte[] GenerateInvoicePdf(SaleResponseDto sale);
    }
}
