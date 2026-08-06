using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class StockAdjustmentLog
    {
        [Key]
        public int AdjustmentID { get; set; }

        [Required]
        public int ConsumableID { get; set; }

        [Required]
        public int PreviousQuantity { get; set; }

        [Required]
        public int NewQuantity { get; set; }

        [Required]
        public int AdjustmentAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string AdjustmentType { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        [Required]
        public int AdjustedBy { get; set; }

        [Required]
        public DateTime AdjustmentDate { get; set; }

        public int? RelatedOrderID { get; set; }

        public int? RelatedTestID { get; set; }

        // Navigation properties
        [ForeignKey("ConsumableID")]
        public virtual Consumable Consumable { get; set; }

        [ForeignKey("AdjustedBy")]
        public virtual User AdjustedByUser { get; set; }

        [ForeignKey("RelatedOrderID")]
        public virtual PurchaseOrder RelatedOrder { get; set; }
    }
}
