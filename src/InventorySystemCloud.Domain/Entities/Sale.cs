using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventorySystemCloud.Domain.Entities
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }

        public int ClientId { get; set; }
        
        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public int? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public User? CreatedBy { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Efectivo";

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ChangeDue { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "COP";

        public virtual ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    }
}
