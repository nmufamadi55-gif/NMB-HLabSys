using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class SampleType
    {
        [Key]
        public int SampleTypeID { get; set; }

        [Required]
        [StringLength(100)]
        public string SampleTypeName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string CollectionTubeType { get; set; }

        [StringLength(500)]
        public string StorageRequirements { get; set; }

        public bool IsActive { get; set; }

        // Navigation property
        public virtual ICollection<TestType> TestTypes { get; set; }
    }
}
