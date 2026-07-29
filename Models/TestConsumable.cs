using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class TestConsumable
    {
        [Key]
        public int TestConsumableID { get; set; }

        [Required]
        public int TestID { get; set; }

        [Required]
        public int ConsumableID { get; set; }

        [Required]
        public int QuantityRequired { get; set; }

        // Navigation properties
        [ForeignKey("TestID")]
        public virtual TestType TestType { get; set; }

        [ForeignKey("ConsumableID")]
        public virtual Consumable Consumable { get; set; }
    }
}
