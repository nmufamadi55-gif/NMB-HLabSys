using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class TestType
    {
        [Key]
        public int TestID { get; set; }

        [Required]
        [StringLength(200)]
        public string TestName { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [Required]
        public int SampleTypeID { get; set; }

        [StringLength(50)]
        public string UnitOfMeasurement { get; set; }

        public decimal? NormalRangeMin { get; set; }

        public decimal? NormalRangeMax { get; set; }

        [Required]
        public int TurnaroundTimeMinutes { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CategoryID")]
        public virtual TestCategory Category { get; set; }

        [ForeignKey("SampleTypeID")]
        public virtual SampleType SampleType { get; set; }

        public virtual ICollection<TechnicianTestAssignment> TechnicianTestAssignments { get; set; }
        public virtual ICollection<TestConsumable> TestConsumables { get; set; }
    }
}