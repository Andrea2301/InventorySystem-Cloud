using System;
using System.Globalization;
using InventorySystemCloud.Application.DTOs.Sales;
using InventorySystemCloud.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InventorySystemCloud.Infrastructure.Services
{
    public class PdfInvoiceGenerator : IPdfInvoiceGenerator
    {
        public byte[] GenerateInvoicePdf(SaleResponseDto sale)
        {
            var culture = new CultureInfo("es-CO");

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

        private static void ComposeHeader(IContainer container, SaleResponseDto sale)
        {
            container.Row(row =>
            {
                // Left: Company Branding
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("InventorySystem Cloud")
                        .FontSize(20).Bold().FontColor(Color.FromHex("#1E3A8A"));
                    col.Item().Text("Comercializadora & Retail S.A.S.")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                    col.Item().Text("NIT: 901.234.567-8 | Régimen Común")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text("Dirección: Cra. 10 # 45-20, Bogotá D.C.")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });

                // Right: Invoice Metadata Box
                row.ConstantItem(180).Border(1).BorderColor(Color.FromHex("#1E3A8A"))
                    .Background(Color.FromHex("#F0F4F8")).Padding(10).Column(col =>
                    {
                        col.Item().AlignCenter().Text("FACTURA DE VENTA")
                            .FontSize(11).Bold().FontColor(Color.FromHex("#1E3A8A"));
                        col.Item().AlignCenter().Text($"FAC-{sale.Id:D6}")
                            .FontSize(13).Bold().FontColor(Colors.Red.Medium);
                        col.Item().PaddingTop(4).Text($"Fecha: {sale.SaleDate:dd/MM/yyyy HH:mm}")
                            .FontSize(8);
                        col.Item().Text($"Método: {sale.PaymentMethod}")
                            .FontSize(8);
                    });
            });
        }

        private static void ComposeContent(IContainer container, SaleResponseDto sale, CultureInfo culture)
        {
            container.PaddingTop(15).Column(col =>
            {
                // Info block (Client and Seller)
                col.Item().Row(row =>
                {
                    // Client info
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("DATOS DEL CLIENTE").Bold().FontSize(9).FontColor(Color.FromHex("#1E3A8A"));
                            c.Item().Text($"Nombre: {sale.ClientName}").FontSize(9);
                            c.Item().Text($"Documento: {sale.ClientDocument}").FontSize(9);
                        });

                    row.ConstantItem(10); // Spacing

                    // Cashier / User info
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Text("INFORMACIÓN DE ATENCIÓN").Bold().FontSize(9).FontColor(Color.FromHex("#1E3A8A"));
                            c.Item().Text($"Atendido por: {sale.CreatedByEmail ?? "Cajero General"}").FontSize(9);
                            c.Item().Text($"Moneda: {sale.Currency}").FontSize(9);
                        });
                });

                col.Item().PaddingTop(15);

                // Items Table
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

                    // Table Header
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

                    // Table Rows
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
                            .Text(item.UnitPrice.ToString("C2", culture)).FontSize(9);
                        table.Cell().Background(bgColor).Padding(5).AlignRight()
                            .Text(item.TotalPrice.ToString("C2", culture)).Bold().FontSize(9);

                        index++;
                    }
                });

                col.Item().PaddingTop(10);

                // Financial Summary Block
                col.Item().AlignRight().Width(220).Column(summary =>
                {
                    summary.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Row(r =>
                    {
                        r.RelativeItem().Text("Total Venta:").Bold().FontSize(11);
                        r.RelativeItem().AlignRight().Text(sale.TotalAmount.ToString("C2", culture))
                            .Bold().FontSize(12).FontColor(Color.FromHex("#1E3A8A"));
                    });

                    summary.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Row(r =>
                    {
                        r.RelativeItem().Text("Monto Recibido:").FontSize(9);
                        r.RelativeItem().AlignRight().Text(sale.AmountPaid.ToString("C2", culture)).FontSize(9);
                    });

                    summary.Item().PaddingVertical(3).Row(r =>
                    {
                        r.RelativeItem().Text("Cambio / Vuelto:").Bold().FontSize(10).FontColor(Colors.Green.Darken2);
                        r.RelativeItem().AlignRight().Text(sale.ChangeDue.ToString("C2", culture))
                            .Bold().FontSize(10).FontColor(Colors.Green.Darken2);
                    });
                });
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Text("¡Gracias por su compra! Este documento es una representación digital de comprobante de venta.")
                        .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                    row.ConstantItem(80).AlignRight().Text(text =>
                    {
                        text.Span("Pág. ").FontSize(8);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" de ").FontSize(8);
                        text.TotalPages().FontSize(8);
                    });
                });
            });
        }
    }
}
