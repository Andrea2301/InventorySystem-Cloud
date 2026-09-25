using System;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace InventorySystemCloud.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly IEmailGenerator _emailGenerator;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> settings,
            IEmailGenerator emailGenerator,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _emailGenerator = emailGenerator;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes = null,
            string? attachmentFileName = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            // Simulation mode or missing credentials
            if (_settings.IsSimulationMode ||
                string.IsNullOrWhiteSpace(_settings.SmtpUser) ||
                string.IsNullOrWhiteSpace(_settings.SmtpPassword))
            {
                _logger.LogInformation(
                    "[EMAIL SIMULATION] Destinatario: {ToEmail} | Asunto: {Subject} | Adjunto: {Attachment}",
                    toEmail, subject, attachmentFileName ?? "Ninguno");
                _logger.LogInformation("[EMAIL SIMULATION] Contenido HTML:\n{Html}", htmlBody);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

                if (attachmentBytes != null && !string.IsNullOrEmpty(attachmentFileName))
                {
                    bodyBuilder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse("application/pdf"));
                }

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("[EMAIL] Correo enviado exitosamente a {ToEmail} vía SMTP ({Host})", toEmail, _settings.SmtpHost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMAIL] Error al enviar correo a {ToEmail}: {Message}", toEmail, ex.Message);
            }
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var html = _emailGenerator.GenerateWelcomeEmail(userName, toEmail);
            await SendEmailAsync(toEmail, "¡Bienvenido a InventorySystem Cloud!", html);
        }

        public async Task SendInvoiceEmailAsync(string toEmail, SaleResponseDto sale, byte[] pdfBytes)
        {
            var html = _emailGenerator.GenerateInvoiceEmail(sale);
            var fileName = $"Factura_Venta_FAC-{sale.Id:D6}.pdf";
            await SendEmailAsync(toEmail, $"Tu Factura Digital FAC-{sale.Id:D6} - InventorySystem Cloud", html, pdfBytes, fileName);
        }
    }
}
