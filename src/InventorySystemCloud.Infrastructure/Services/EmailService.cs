using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventorySystemCloud.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private const string ResendApiUrl = "https://api.resend.com/emails";
        private readonly EmailSettings _settings;
        private readonly IEmailGenerator _emailGenerator;
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> settings,
            IEmailGenerator emailGenerator,
            HttpClient httpClient,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _emailGenerator = emailGenerator;
            _httpClient = httpClient;
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

            // Simulation mode or missing API key
            if (_settings.IsSimulationMode || string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey.StartsWith("re_YOUR"))
            {
                _logger.LogInformation(
                    "[RESEND SIMULATION] Destinatario: {ToEmail} | Asunto: {Subject} | Adjunto: {Attachment}",
                    toEmail, subject, attachmentFileName ?? "Ninguno");
                return;
            }

            try
            {
                var fromAddress = string.IsNullOrWhiteSpace(_settings.SenderName)
                    ? _settings.SenderEmail
                    : $"{_settings.SenderName} <{_settings.SenderEmail}>";

                var payload = new ResendEmailPayload
                {
                    From = fromAddress,
                    To = new List<string> { toEmail },
                    Subject = subject,
                    Html = htmlBody
                };

                if (attachmentBytes != null && !string.IsNullOrEmpty(attachmentFileName))
                {
                    payload.Attachments = new List<ResendAttachment>
                    {
                        new()
                        {
                            Filename = attachmentFileName,
                            Content = Convert.ToBase64String(attachmentBytes)
                        }
                    };
                }

                var request = new HttpRequestMessage(HttpMethod.Post, ResendApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[RESEND] Correo enviado exitosamente a {ToEmail} vía HTTPS API", toEmail);
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[RESEND] Error en la API de Resend ({StatusCode}): {Body}", response.StatusCode, errorBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RESEND] Excepción al enviar correo a {ToEmail}", toEmail);
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

        private class ResendEmailPayload
        {
            [JsonPropertyName("from")]
            public string From { get; set; } = string.Empty;

            [JsonPropertyName("to")]
            public List<string> To { get; set; } = new();

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("html")]
            public string Html { get; set; } = string.Empty;

            [JsonPropertyName("attachments")]
            public List<ResendAttachment>? Attachments { get; set; }
        }

        private class ResendAttachment
        {
            [JsonPropertyName("filename")]
            public string Filename { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }
    }
}
