using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using InventorySystemCloud.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventorySystemCloud.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly IAppDbContext _context;

        public ReportService(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateProductsExcelReportAsync()
        {
            var products = await _context.Products
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inventario de Productos");

            // Title Banner
            worksheet.Cell(1, 1).Value = "INVENTORYSYSTEM CLOUD - REPORTE DE INVENTARIO";
            worksheet.Range(1, 1, 1, 8).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(31, 78, 121);
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(2, 1).Value = $"Generado el: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC | Total Items: {products.Count}";
            worksheet.Range(2, 1, 2, 8).Merge();
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 9;
            worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Table Headers
            string[] headers = { "ID", "Nombre del Producto", "Categoría", "Precio Unitario", "Stock", "Valor Total", "Estado", "Fecha Registro" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 117, 182);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 5;
            foreach (var p in products)
            {
                worksheet.Cell(row, 1).Value = p.Id;
                worksheet.Cell(row, 2).Value = p.Name;
                worksheet.Cell(row, 3).Value = p.Category;
                worksheet.Cell(row, 4).Value = p.Price;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "$ #,##0.00";
                worksheet.Cell(row, 5).Value = p.Quantity;
                worksheet.Cell(row, 6).FormulaA1 = $"D{row}*E{row}";
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "$ #,##0.00";
                worksheet.Cell(row, 7).Value = p.Status;
                worksheet.Cell(row, 8).Value = p.CreatedAt.ToString("yyyy-MM-dd HH:mm");

                // Highlight low stock or inactive
                if (!p.IsActive)
                {
                    worksheet.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                    worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.FromArgb(128, 128, 128);
                }
                else if (p.Quantity <= 5)
                {
                    worksheet.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 230, 230);
                    worksheet.Cell(row, 5).Style.Font.FontColor = XLColor.FromArgb(192, 0, 0);
                    worksheet.Cell(row, 5).Style.Font.Bold = true;
                }

                row++;
            }

            // Totals Row
            if (products.Count > 0)
            {
                worksheet.Cell(row, 2).Value = "TOTALES GENERALES:";
                worksheet.Cell(row, 2).Style.Font.Bold = true;
                worksheet.Cell(row, 5).FormulaA1 = $"SUM(E5:E{row - 1})";
                worksheet.Cell(row, 5).Style.Font.Bold = true;
                worksheet.Cell(row, 6).FormulaA1 = $"SUM(F5:F{row - 1})";
                worksheet.Cell(row, 6).Style.Font.Bold = true;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "$ #,##0.00";
                worksheet.Range(row, 1, row, 8).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range(row, 1, row, 8).Style.Border.BottomBorder = XLBorderStyleValues.Double;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateProductsCsvReportAsync()
        {
            var products = await _context.Products
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var sb = new StringBuilder();
            // UTF-8 CSV header
            sb.AppendLine("ID;Nombre;Categoria;Precio;Stock;ValorTotal;Estado;FechaRegistro");

            foreach (var p in products)
            {
                var totalVal = p.Price * p.Quantity;
                var safeName = p.Name.Replace(";", ",");
                var safeCategory = p.Category.Replace(";", ",");
                sb.AppendLine($"{p.Id};\"{safeName}\";\"{safeCategory}\";{p.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)};{p.Quantity};{totalVal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)};\"{p.Status}\";{p.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            // Return with UTF-8 BOM so Excel opens accents correctly
            var utf8Bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            return utf8Bom.Concat(contentBytes).ToArray();
        }

        public async Task<byte[]> GenerateSalesReportAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Sales
                .Include(s => s.Client)
                .Include(s => s.CreatedBy)
                .Include(s => s.SaleDetails)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.SaleDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.SaleDate <= endDate.Value);

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ventas");

            // Title Banner
            worksheet.Cell(1, 1).Value = "INVENTORYSYSTEM CLOUD - REPORTE DE VENTAS";
            worksheet.Range(1, 1, 1, 8).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(31, 78, 121);
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var filterInfo = $"Filtro: Desde {(startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : "Inicio")} Hasta {(endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : "Actual")} | Total Transacciones: {sales.Count}";
            worksheet.Cell(2, 1).Value = filterInfo;
            worksheet.Range(2, 1, 2, 8).Merge();
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 9;
            worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Headers
            string[] headers = { "Factura #", "Fecha y Hora", "Cliente", "Documento", "Vendedor", "Método Pago", "Cant. Artículos", "Total Venta" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 117, 182);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 5;
            foreach (var s in sales)
            {
                worksheet.Cell(row, 1).Value = $"FAC-{s.Id:D6}";
                worksheet.Cell(row, 2).Value = s.SaleDate.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 3).Value = s.Client != null ? s.Client.FullName : "Consumidor Final";
                worksheet.Cell(row, 4).Value = s.Client?.DocumentNumber ?? "N/A";
                worksheet.Cell(row, 5).Value = s.CreatedBy?.Email ?? "Sistema";
                worksheet.Cell(row, 6).Value = s.PaymentMethod;
                worksheet.Cell(row, 7).Value = s.SaleDetails.Sum(d => d.Quantity);
                worksheet.Cell(row, 8).Value = s.TotalAmount;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "$ #,##0.00";
                row++;
            }

            // Totals Row
            if (sales.Count > 0)
            {
                worksheet.Cell(row, 6).Value = "TOTAL GENERAL:";
                worksheet.Cell(row, 6).Style.Font.Bold = true;
                worksheet.Cell(row, 7).FormulaA1 = $"SUM(G5:G{row - 1})";
                worksheet.Cell(row, 7).Style.Font.Bold = true;
                worksheet.Cell(row, 8).FormulaA1 = $"SUM(H5:H{row - 1})";
                worksheet.Cell(row, 8).Style.Font.Bold = true;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "$ #,##0.00";
                worksheet.Range(row, 1, row, 8).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range(row, 1, row, 8).Style.Border.BottomBorder = XLBorderStyleValues.Double;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GeneratePurchasesReportAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.CreatedBy)
                .Include(p => p.PurchaseDetails)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.PurchaseDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.PurchaseDate <= endDate.Value);

            var purchases = await query
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Compras");

            // Title Banner
            worksheet.Cell(1, 1).Value = "INVENTORYSYSTEM CLOUD - REPORTE DE COMPRAS";
            worksheet.Range(1, 1, 1, 8).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(31, 78, 121);
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var filterInfo = $"Filtro: Desde {(startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : "Inicio")} Hasta {(endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : "Actual")} | Total Compras: {purchases.Count}";
            worksheet.Cell(2, 1).Value = filterInfo;
            worksheet.Range(2, 1, 2, 8).Merge();
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 9;
            worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Headers
            string[] headers = { "Compra #", "Fecha y Hora", "Proveedor", "Email Proveedor", "Factura Prov.", "Registrado Por", "Cant. Artículos", "Total Compra" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 117, 182);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 5;
            foreach (var p in purchases)
            {
                worksheet.Cell(row, 1).Value = $"COM-{p.Id:D6}";
                worksheet.Cell(row, 2).Value = p.PurchaseDate.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 3).Value = p.Supplier?.CompanyName ?? "Proveedor General";
                worksheet.Cell(row, 4).Value = p.Supplier?.Email ?? "N/A";
                worksheet.Cell(row, 5).Value = p.InvoiceNumber ?? "N/A";
                worksheet.Cell(row, 6).Value = p.CreatedBy?.Email ?? "Sistema";
                worksheet.Cell(row, 7).Value = p.PurchaseDetails.Sum(d => d.Quantity);
                worksheet.Cell(row, 8).Value = p.TotalAmount;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "$ #,##0.00";
                row++;
            }

            // Totals Row
            if (purchases.Count > 0)
            {
                worksheet.Cell(row, 6).Value = "TOTAL GENERAL:";
                worksheet.Cell(row, 6).Style.Font.Bold = true;
                worksheet.Cell(row, 7).FormulaA1 = $"SUM(G5:G{row - 1})";
                worksheet.Cell(row, 7).Style.Font.Bold = true;
                worksheet.Cell(row, 8).FormulaA1 = $"SUM(H5:H{row - 1})";
                worksheet.Cell(row, 8).Style.Font.Bold = true;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "$ #,##0.00";
                worksheet.Range(row, 1, row, 8).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                worksheet.Range(row, 1, row, 8).Style.Border.BottomBorder = XLBorderStyleValues.Double;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateAuditLogsReportAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Auditoría");

            // Title Banner
            worksheet.Cell(1, 1).Value = "INVENTORYSYSTEM CLOUD - REPORTE DE AUDITORÍA Y TRAZABILIDAD";
            worksheet.Range(1, 1, 1, 5).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(31, 78, 121);
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var filterInfo = $"Filtro: Desde {(startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : "Inicio")} Hasta {(endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : "Actual")} | Total Registros: {logs.Count}";
            worksheet.Cell(2, 1).Value = filterInfo;
            worksheet.Range(2, 1, 2, 5).Merge();
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 9;
            worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Headers
            string[] headers = { "ID", "Fecha y Hora (UTC)", "Usuario", "Acción Realizada", "Detalles" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 117, 182);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 5;
            foreach (var l in logs)
            {
                worksheet.Cell(row, 1).Value = l.Id;
                worksheet.Cell(row, 2).Value = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 3).Value = l.User?.Email ?? $"User #{l.UserId}";
                worksheet.Cell(row, 4).Value = l.Action;
                worksheet.Cell(row, 5).Value = l.Details ?? string.Empty;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
