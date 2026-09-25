using System;
using System.Globalization;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Application.Interfaces;
using InventorySystemCloud.Application.Settings;
using InventorySystemCloud.Shared;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventorySystemCloud.Infrastructure.Services
{
    public class PdfInvoiceGenerator : IPdfInvoiceGenerator
    {
        private readonly CompanySettings _companySettings;

        public PdfInvoiceGenerator(IOptions<CompanySettings>? companySettings = null)
        {
            _companySettings = companySettings?.Value ?? new CompanySettings();
        }

        public byte[] GenerateInvoicePdf(SaleResponseDto sale)
        {
            var culture = CurrencyHelper.GetCultureInfo(sale.Currency);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                    page.Header().Element(header => ComposeHeader(header, sale));
                    page.Content().Element(content => ComposeContent(content, sale, culture));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, SaleResponseDto sale)
        {
            container.Row(row =>
            {
                // Izquierda: Branding Internacional de la Empresa
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(_companySettings.CompanyName)
                        .FontSize(20).Bold().FontColor(Color.FromHex("#1E3A8A"));
                    col.Item().Text(_companySettings.LegalName)
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                    col.Item().Text($"{_companySettings.TaxIdLabel}: {_companySettings.TaxIdNumber}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(_companySettings.Address)
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });

                // Derecha: Bloque de metadatos de la factura
                row.ConstantItem(180).Border(1).BorderColor(Color.FromHex("#1E3A8A"))
                    .Background(Color.FromHex("#F0F4F8")).Padding(10).Column(col =>
                    {
                        col.Item().AlignCenter().Text("FACTURA DE VENTA / INVOICE")
                            .FontSize(10).Bold().FontColor(Color.FromHex("#1E3A8A"));
                        col.Item().AlignCenter().Text($"FAC-{sale.Id:D6}")
                            .FontSize(13).Bold().FontColor(Colors.Red.Medium);
                        col.Item().PaddingTop(4).Text($"Fecha: {sale.SaleDate:yyyy-MM-dd HH:mm}")
                            .FontSize(8);
                        col.Item().Text($"Método: {sale.PaymentMethod}")
                            .FontSize(8);
                        col.Item().Text($"Moneda: {sale.Currency}")
                            .FontSize(8).Bold();
                    });
            });
        }

        private static void ComposeContent(IContainer container, SaleResponseDto sale, CultureInfo culture)
        {
            container.PaddingTop(15).Column(col =>
            {
                // Bloque de información (Cliente y Vendedor)
                col.Item().Row(row =>
                {
                    // Info del Cliente
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("DATOS DEL CLIENTE / CLIENT INFO").Bold().FontSize(9).FontColor(Color.FromHex("#1E3A8A"));
                            c.Item().Text($"Nombre: {sale.ClientName}").FontSize(9);
                            c.Item().Text($"Documento / Tax ID: {sale.ClientDocument}").FontSize(9);
                        });

                    row.ConstantItem(10); // Espaciador

                    // Info del Cajero / Usuario
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("INFORMACIÓN DE ATENCIÓN").Bold().FontSize(9).FontColor(Color.FromHex("#1E3A8A"));
                            c.Item().Text($"Atendido por: {sale.CreatedByEmail ?? "Cajero General"}").FontSize(9);
                            c.Item().Text($"Moneda de Operación: {sale.Currency}").FontSize(9);
                        });
                });

                col.Item().PaddingTop(15);

                // Tabla de Artículos
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);  // #
                        columns.RelativeColumn(4);   // Producto
                        columns.RelativeColumn(2);   // Cantidad
                        columns.RelativeColumn(2);   // Precio Unitario
                        columns.RelativeColumn(2);   // Total
                    });

                    // Encabezado de la Tabla
                    table.Header(header =>
                    {
                        header.Cell().Background(Color.FromHex("#1E3A8A")).Padding(5).AlignCenter()
                            .Text("#").Bold().FontColor(Colors.White).FontSize(9);
                        header.Cell().Background(Color.FromHex("#1E3A8A")).Padding(5)
                            .Text("Producto / Descripción").Bold().FontColor(Colors.White).FontSize(9);
                        header.Cell().Background(Color.FromHex("#1E3A8A")).Padding(5).AlignCenter()
                            .Text("Cant.").Bold().FontColor(Colors.White).FontSize(9);
                        header.Cell().Background(Color.FromHex("#1E3A8A")).Padding(5).AlignRight()
                            .Text("P. Unitario").Bold().FontColor(Colors.White).FontSize(9);
                        header.Cell().Background(Color.FromHex("#1E3A8A")).Padding(5).AlignRight()
                            .Text("Total").Bold().FontColor(Colors.White).FontSize(9);
                    });

                    // Filas de la Tabla
                    var index = 1;
                    foreach (var item in sale.Items)
                    {
                        var bgColor = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                        table.Cell().Background(bgColor).Padding(5).AlignCenter()
                            .Text(index.ToString()).FontSize(9);
                        table.Cell().Background(bgColor).Padding(5)
                            .Text(item.ProductName).FontSize(9);
                        table.Cell().Background(bgColor).Padding(5).AlignCenter()
                            .Text(item.Quantity.ToString()).FontSize(9);
                        table.Cell().Background(bgColor).Padding(5).AlignRight()
                            .Text(CurrencyHelper.FormatAmount(item.UnitPrice, sale.Currency)).FontSize(9);
                        table.Cell().Background(bgColor).Padding(5).AlignRight()
                            .Text(CurrencyHelper.FormatAmount(item.TotalPrice, sale.Currency)).Bold().FontSize(9);

                        index++;
                    }
                });

                col.Item().PaddingTop(10);

                // Bloque de Resumen Financiero
                col.Item().AlignRight().Width(240).Column(summary =>
                {
                    summary.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Row(r =>
                    {
                        r.RelativeItem().Text("Total Venta:").Bold().FontSize(11);
                        r.RelativeItem().AlignRight().Text(CurrencyHelper.FormatAmount(sale.TotalAmount, sale.Currency))
                            .Bold().FontSize(12).FontColor(Color.FromHex("#1E3A8A"));
                    });

                    summary.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Row(r =>
                    {
                        r.RelativeItem().Text("Monto Recibido:").FontSize(9);
                        r.RelativeItem().AlignRight().Text(CurrencyHelper.FormatAmount(sale.AmountPaid, sale.Currency)).FontSize(9);
                    });

                    // Línea del Cambio / Vuelto
                    summary.Item().PaddingVertical(3).Row(r =>
                    {
                        r.RelativeItem().Text("Cambio / Vuelto:").Bold().FontSize(10);
                        r.RelativeItem().AlignRight().Text(CurrencyHelper.FormatAmount(sale.ChangeDue, sale.Currency))
                            .Bold().FontSize(10).FontColor(Colors.Green.Darken2);
                    });
                });
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(col =>
            {
                col.Item().Text("Gracias por su compra / Thank you for your business").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                col.Item().Text(x =>
                {
                    x.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }
    }
}
