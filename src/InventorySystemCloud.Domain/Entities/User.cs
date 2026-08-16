using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventorySystemCloud.Domain.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.NewGuid();

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;


        [Required]
        public UserRole Role { get; set; } = UserRole.Cashier;

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        [Required]
        [MaxLength(64)]
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

        public int FailedLoginAttempts { get; set; }

        public DateTime? LockoutEnd { get; set; }

        // Navigation
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();

        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        [NotMapped]
        public string RoleDisplay =>
            Role == UserRole.Admin ? "Administrator" : "Cashier";
    }
}
