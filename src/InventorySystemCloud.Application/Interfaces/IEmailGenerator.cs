using InventorySystemCloud.Application.DTOs.Sales;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IEmailGenerator
    {
        string GenerateWelcomeEmail(string name, string email);
        string GenerateInvoiceEmail(SaleResponseDto sale);
    }
}
