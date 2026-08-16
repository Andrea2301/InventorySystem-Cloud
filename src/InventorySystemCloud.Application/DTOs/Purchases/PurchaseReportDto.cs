using System;

namespace InventorySystemCloud.Application.DTOs.Purchases
{
    public class PurchaseReportDto
    {
        public DateTime Date { get; set; }
        public int TotalPurchasesCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AveragePurchaseCost { get; set; }
        public int TotalItemsPurchased { get; set; }
    }
}
