using System;
using System.Collections.Generic;

namespace InventorySystemCloud.Application.DTOs.Purchases
{
    public class PurchaseResponseDto
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierEmail { get; set; } = string.Empty;
        public int? CreatedByUserId { get; set; }
        public string? CreatedByEmail { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Notes { get; set; }
        public string Currency { get; set; } = "COP";
        public List<PurchaseDetailResponseDto> Items { get; set; } = new List<PurchaseDetailResponseDto>();
    }
}
