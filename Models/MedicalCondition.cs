using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class MedicalCondition
    {
        [Key]
        public int ConditionID { get; set; }

        [Required]
        [StringLength(200)]
        public string ConditionName { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        [ForeignKey("CategoryID")]
        public virtual LookupCategory Category { get; set; }

        public virtual ICollection<PatientCondition> PatientConditions { get; set; }
    }
}
