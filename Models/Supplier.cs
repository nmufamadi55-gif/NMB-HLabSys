using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }

        [Required]
        [StringLength(200)]
        public string SupplierName { get; set; }

        [Required]
        [StringLength(150)]
        public string ContactPerson { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string EmailAddress { get; set; }

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public bool IsPreferred { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Consumable> Consumables { get; set; }
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; }
    }
}
