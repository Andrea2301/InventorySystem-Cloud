using System.ComponentModel.DataAnnotations;

namespace InventorySystemCloud.Domain.Entities
{
    public class UserPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Permission { get; set; } = string.Empty;

        // Navigation
        public User User { get; set; } = null!;
    }
}
