using System.Globalization;
using System.Text;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Application.Interfaces;

namespace InventorySystemCloud.Infrastructure.Services
{
    public class EmailGenerator : IEmailGenerator
    {
        public string GenerateWelcomeEmail(string name, string email)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html lang='es'><head><meta charset='UTF-8'>");
            sb.Append("<style>");
            sb.Append("body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.08); }");
            sb.Append(".header { background: linear-gradient(135deg, #1E3A8A, #3B82F6); padding: 30px 20px; text-align: center; color: #ffffff; }");
            sb.Append(".header h1 { margin: 0; font-size: 24px; font-weight: 700; }");
            sb.Append(".content { padding: 30px 25px; line-height: 1.6; }");
            sb.Append(".button { display: inline-block; background-color: #1E3A8A; color: #ffffff !important; text-decoration: none; padding: 12px 25px; border-radius: 6px; font-weight: 600; margin-top: 15px; }");
            sb.Append(".card { background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 15px; margin: 20px 0; }");
            sb.Append(".footer { background-color: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }");
            sb.Append("</style></head><body>");

            sb.Append("<div class='container'>");
            sb.Append("<div class='header'>");
            sb.Append("<h1>¡Bienvenido a InventorySystem Cloud!</h1>");
            sb.Append("</div>");

            sb.Append("<div class='content'>");
            sb.Append($"<p>Hola <strong>{name}</strong>,</p>");
            sb.Append("<p>Tu cuenta ha sido creada exitosamente en la plataforma <strong>InventorySystem Cloud</strong>. Ahora puedes gestionar inventarios, ventas, compras y clientes en tiempo real.</p>");

            sb.Append("<div class='card'>");
            sb.Append("<p style='margin: 0 0 8px 0;'><strong>Detalles de tu cuenta:</strong></p>");
            sb.Append($"<p style='margin: 0;'>📧 Correo: <code>{email}</code></p>");
            sb.Append("</div>");

            sb.Append("<p style='text-align: center;'>");
            sb.Append("<a href='http://localhost:4200/login' class='button'>Iniciar Sesión en el Portal</a>");
            sb.Append("</p>");

            sb.Append("<p style='font-size: 13px; color: #64748b;'>Por seguridad, nunca compartas tus credenciales con nadie.</p>");
            sb.Append("</div>");

            sb.Append("<div class='footer'>");
            sb.Append("<p>© 2026 InventorySystem Cloud. Todos los derechos reservados.<br>Este es un correo automático, por favor no respondas a este mensaje.</p>");
            sb.Append("</div>");
            sb.Append("</div></body></html>");

            return sb.ToString();
        }

        public string GenerateInvoiceEmail(SaleResponseDto sale)
        {
            var culture = new CultureInfo("es-CO");
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html>");
            sb.Append("<html lang='es'><head><meta charset='UTF-8'>");
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
            sb.Append($"<p style='margin: 3px 0;'><strong>Fecha:</strong> {sale.SaleDate:dd/MM/yyyy HH:mm}</p>");
            sb.Append($"<p style='margin: 3px 0;'><strong>Método de Pago:</strong> {sale.PaymentMethod}</p>");
            sb.Append($"<p style='margin: 3px 0;'><strong>Ítems comprados:</strong> {sale.Items.Count} producto(s)</p>");
            sb.Append($"<p style='margin: 8px 0 0 0; font-size: 16px;' class='total'>Total: {sale.TotalAmount.ToString("C2", culture)} {sale.Currency}</p>");
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