using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class PurchaseOrder
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; }

        [Required]
        public int SupplierID { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public string CancellationReason { get; set; }

        public DateTime? DateCompleted { get; set; }

        public DateTime? DateCancelled { get; set; }

        public decimal? TotalAmount { get; set; }

        public int CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("SupplierID")]
        public virtual Supplier Supplier { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User CreatedByUser { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}
