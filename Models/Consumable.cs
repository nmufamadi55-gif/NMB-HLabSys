using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class Consumable
    {
        [Key]
        public int ConsumableID { get; set; }

        [Required]
        [StringLength(200)]
        public string ConsumableName { get; set; }

        [Required]
        public int SupplierID { get; set; }

        [Required]
        public int ReorderLevel { get; set; }

        [Required]
        public int QuantityOnHand { get; set; }

        [StringLength(20)]
        public string UnitOfMeasure { get; set; }

        public decimal? UnitPrice { get; set; }

        public int MinimumStockLevel { get; set; }

        [StringLength(100)]
        public string StorageLocation { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("SupplierID")]
        public virtual Supplier Supplier { get; set; }

        public virtual ICollection<TestConsumable> TestConsumables { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<StockAdjustmentLog> StockAdjustmentLogs { get; set; }
    }
}
