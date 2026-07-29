using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        public int ConsumableID { get; set; }

        [Required]
        public int QuantityOrdered { get; set; }

        public int QuantityReceived { get; set; }

        public decimal? UnitPrice { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public string CancellationReason { get; set; }

        public DateTime? DateReceived { get; set; }

        // Navigation properties
        [ForeignKey("OrderID")]
        public virtual PurchaseOrder Order { get; set; }

        [ForeignKey("ConsumableID")]
        public virtual Consumable Consumable { get; set; }
    }
}
