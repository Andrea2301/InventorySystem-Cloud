using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Sales;

namespace InventorySystemCloud.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? attachmentBytes = null, string? attachmentFileName = null);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
        Task SendInvoiceEmailAsync(string toEmail, SaleResponseDto sale, byte[] pdfBytes);
    }
}
