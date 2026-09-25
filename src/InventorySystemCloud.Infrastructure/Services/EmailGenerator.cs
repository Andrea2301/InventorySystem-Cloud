using System;
using System.Text;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Shared;

namespace InventorySystemCloud.Infrastructure.Services
{
    public class EmailGenerator : IEmailGenerator
    {
        public string GenerateWelcomeEmail(string userName, string userEmail)
        {
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html>");
            sb.Append("<html lang='es'>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8'>");
            sb.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.Append("<title>Bienvenido a InventorySystem Cloud</title>");
            sb.Append("<style>");
            sb.Append("body { margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #1e293b; }");
            sb.Append("table { border-collapse: collapse; }");
            sb.Append(".wrapper { width: 100%; table-layout: fixed; background-color: #f1f5f9; padding: 40px 15px; }");
            sb.Append(".main-card { max-width: 620px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.08), 0 8px 10px -6px rgba(15, 23, 42, 0.04); border: 1px solid #e2e8f0; }");
            sb.Append(".header { background: linear-gradient(135deg, #0f172a 0%, #1e3a8a 50%, #2563eb 100%); padding: 45px 30px 40px; text-align: center; color: #ffffff; }");
            sb.Append(".badge { display: inline-block; background: rgba(255, 255, 255, 0.15); border: 1px solid rgba(255, 255, 255, 0.25); padding: 6px 14px; border-radius: 9999px; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 1.5px; margin-bottom: 16px; backdrop-filter: blur(4px); }");
            sb.Append(".header-title { margin: 0; font-size: 26px; font-weight: 800; letter-spacing: -0.5px; line-height: 1.25; }");
            sb.Append(".header-subtitle { margin: 10px 0 0; font-size: 14px; color: #cbd5e1; line-height: 1.5; font-weight: 400; }");
            sb.Append(".body-content { padding: 35px 32px; }");
            sb.Append(".greeting { font-size: 17px; font-weight: 600; color: #0f172a; margin-top: 0; margin-bottom: 12px; }");
            sb.Append(".lead-text { font-size: 14.5px; line-height: 1.65; color: #475569; margin: 0 0 24px; }");
            sb.Append(".account-box { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 18px 20px; margin-bottom: 28px; }");
            sb.Append(".account-row { display: table; width: 100%; }");
            sb.Append(".account-label { font-size: 11.5px; font-weight: 700; text-transform: uppercase; color: #64748b; letter-spacing: 0.5px; margin-bottom: 4px; }");
            sb.Append(".account-val { font-size: 15px; font-weight: 700; color: #0f172a; word-break: break-all; }");
            sb.Append(".feature-grid { margin-bottom: 28px; }");
            sb.Append(".feature-item { background-color: #ffffff; border: 1px solid #f1f5f9; border-radius: 10px; padding: 14px 16px; margin-bottom: 10px; }");
            sb.Append(".feature-title { font-size: 13.5px; font-weight: 700; color: #1e293b; margin: 0 0 4px; }");
            sb.Append(".feature-desc { font-size: 12.5px; color: #64748b; margin: 0; line-height: 1.45; }");
            sb.Append(".cta-container { text-align: center; margin: 30px 0 20px; }");
            sb.Append(".btn { display: inline-block; background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%); color: #ffffff !important; font-weight: 700; font-size: 14px; text-decoration: none; padding: 14px 32px; border-radius: 10px; box-shadow: 0 4px 12px rgba(37, 99, 235, 0.3); letter-spacing: 0.3px; }");
            sb.Append(".security-tip { background-color: #fffbeb; border: 1px solid #fef3c7; border-left: 4px solid #f59e0b; border-radius: 8px; padding: 12px 16px; font-size: 12px; color: #92400e; line-height: 1.5; margin-top: 24px; }");
            sb.Append(".footer { background-color: #f8fafc; padding: 24px 30px; text-align: center; border-top: 1px solid #e2e8f0; }");
            sb.Append(".footer-brand { font-size: 13px; font-weight: 700; color: #334155; margin: 0 0 6px; }");
            sb.Append(".footer-text { font-size: 11.5px; color: #94a3b8; margin: 0; line-height: 1.5; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='wrapper'>");
            sb.Append("<div class='main-card'>");

            // Header Section
            sb.Append("<div class='header'>");
            sb.Append("<div class='badge'>☁️ InventorySystem Cloud</div>");
            sb.Append("<h1 class='header-title'>¡Bienvenido a la plataforma!</h1>");
            sb.Append("<p class='header-subtitle'>Tu cuenta ha sido configurada y está lista para operar.</p>");
            sb.Append("</div>");

            // Content Section
            sb.Append("<div class='body-content'>");
            sb.Append($"<p class='greeting'>Hola, <strong>{userName}</strong> 👋</p>");
            sb.Append("<p class='lead-text'>Nos alegra darte la bienvenida a <strong>InventorySystem Cloud</strong>, tu solución integral para la administración ágil de inventarios, compras, proveedores y ventas en la nube.</p>");

            // Account details box
            sb.Append("<div class='account-box'>");
            sb.Append("<div class='account-label'>Cuenta de Usuario Registrada</div>");
            sb.Append($"<div class='account-val'>✉️ {userEmail}</div>");
            sb.Append("</div>");

            // Key features highlights
            sb.Append("<div class='feature-grid'>");
            sb.Append("<div class='feature-item'>");
            sb.Append("<div class='feature-title'> Control de Inventario en Tiempo Real</div>");
            sb.Append("<p class='feature-desc'>Monitorea existencias, alertas de stock mínimo y catálogo de productos con imágenes en alta resolución.</p>");
            sb.Append("</div>");

            sb.Append("<div class='feature-item'>");
            sb.Append("<div class='feature-title'> Punto de Venta y Facturación Digital</div>");
            sb.Append("<p class='feature-desc'>Registra ventas con agilidad, calcula cambio automáticamente y emite facturas en formato PDF al instante.</p>");
            sb.Append("</div>");

            sb.Append("<div class='feature-item'>");
            sb.Append("<div class='feature-title'> Reportes y Analíticas Avanzadas</div>");
            sb.Append("<p class='feature-desc'>Exporta informes consolidados de inventario, ventas y compras en formatos Excel y CSV compatibles.</p>");
            sb.Append("</div>");
            sb.Append("</div>");

            // CTA Button
            sb.Append("<div class='cta-container'>");
            sb.Append("<a href='http://localhost:4200' class='btn' target='_blank'>Ir al Panel de Control &rarr;</a>");
            sb.Append("</div>");

            // Security note
            sb.Append("<div class='security-tip'>");
            sb.Append("<strong> Consejo de Seguridad:</strong> Mantén tus credenciales protegidas. El equipo de InventorySystem Cloud nunca te solicitará tu contraseña por correo ni por mensajes directos.");
            sb.Append("</div>");

            sb.Append("</div>"); // End body-content

            // Footer Section
            sb.Append("<div class='footer'>");
            sb.Append("<p class='footer-brand'>InventorySystem Cloud &copy; 2026</p>");
            sb.Append("<p class='footer-text'>Este es un mensaje de notificación automática generado por el sistema de seguridad.<br>Por favor no respondas a este correo.</p>");
            sb.Append("</div>");

            sb.Append("</div>"); // End main-card
            sb.Append("</div>"); // End wrapper
            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }

        public string GenerateInvoiceEmail(SaleResponseDto sale)
        {
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html>");
            sb.Append("<html lang='en'><head><meta charset='UTF-8'>");
            sb.Append("<style>");
            sb.Append("body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.08); }");
            sb.Append(".header { background: linear-gradient(135deg, #0F766E, #14B8A6); padding: 30px 20px; text-align: center; color: #ffffff; }");
            sb.Append(".header h1 { margin: 0; font-size: 22px; font-weight: 700; }");
            sb.Append(".content { padding: 30px 25px; line-height: 1.6; }");
            sb.Append(".card { background-color: #f0fdfa; border: 1px solid #ccfbf1; border-radius: 8px; padding: 15px; margin: 20px 0; }");
            sb.Append(".summary-row { display: flex; justify-content: space-between; margin-bottom: 6px; }");
            sb.Append(".total { font-size: 18px; font-weight: 700; color: #0F766E; }");
            sb.Append(".footer { background-color: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }");
            sb.Append("</style></head><body>");

            sb.Append("<div class='container'>");
            sb.Append("<div class='header'>");
            sb.Append("<h1>Comprobante de Venta Digital</h1>");
            sb.Append("</div>");

            sb.Append("<div class='content'>");
            sb.Append($"<p>Estimado(a) <strong>{sale.ClientName}</strong>,</p>");
            sb.Append("<p>Adjunto a este correo encontrarás la <strong>Factura Digital oficial en formato PDF</strong> correspondiente a tu compra reciente.</p>");

            sb.Append("<div class='card'>");
            sb.Append($"<p style='margin: 0 0 10px 0; font-weight: bold; color: #0F766E;'>Resumen de la Transacción — FAC-{sale.Id:D6}</p>");
            sb.Append($"<p style='margin: 3px 0;'><strong>Fecha:</strong> {sale.SaleDate:yyyy-MM-dd HH:mm}</p>");
            sb.Append($"<p style='margin: 3px 0;'><strong>Método de Pago:</strong> {sale.PaymentMethod}</p>");
            sb.Append($"<p style='margin: 3px 0;'><strong>Moneda:</strong> {sale.Currency}</p>");
            sb.Append($"<p style='margin: 3px 0;'><strong>Ítems comprados:</strong> {sale.Items.Count} producto(s)</p>");
            sb.Append($"<p style='margin: 8px 0 0 0; font-size: 16px;' class='total'>Total: {CurrencyHelper.FormatAmount(sale.TotalAmount, sale.Currency)}</p>");
            sb.Append("</div>");

            sb.Append("<p>¡Agradecemos tu preferencia y esperamos atenderte nuevamente pronto!</p>");
            sb.Append("</div>");

            sb.Append("<div class='footer'>");
            sb.Append("<p>© 2026 InventorySystem Cloud. Todos los derechos reservados.<br>Documento generado electrónicamente con valor de comprobante.</p>");
            sb.Append("</div>");
            sb.Append("</div></body></html>");

            return sb.ToString();
        }
    }
}