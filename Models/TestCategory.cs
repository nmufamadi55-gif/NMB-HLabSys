using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class TestCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property
        public virtual ICollection<TestType> TestTypes { get; set; }
    }
}
