using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class LookupCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        [Required]
        [StringLength(20)]
        public string CategoryType { get; set; } // 'Condition', 'Allergy', 'Medication'

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual ICollection<MedicalCondition> MedicalConditions { get; set; }
        public virtual ICollection<Allergy> Allergies { get; set; }
        public virtual ICollection<Medication> Medications { get; set; }
    }
}