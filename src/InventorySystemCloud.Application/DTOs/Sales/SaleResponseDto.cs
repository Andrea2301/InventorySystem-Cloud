using System;
using System.Collections.Generic;

namespace InventorySystemCloud.Application.DTOs.Sales
{
    public class SaleResponseDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientDocument { get; set; } = string.Empty;
        public int? CreatedByUserId { get; set; }
        public string? CreatedByEmail { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal ChangeDue { get; set; }
        public string Currency { get; set; } = "COP";
        public List<SaleDetailResponseDto> Items { get; set; } = new();
    }

    public class SaleReportDto
    {
        public DateTime Date { get; set; }
        public int TotalSalesCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageTicket { get; set; }
        public int TotalItemsSold { get; set; }
    }
}
