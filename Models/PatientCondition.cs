using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class PatientCondition
    {
        [Key]
        public int PatientConditionID { get; set; }

        [Required]
        public int PatientID { get; set; }

        [Required]
        public int ConditionID { get; set; }

        public DateTime? DiagnosedDate { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("PatientID")]
        public virtual Patient Patient { get; set; }

        [ForeignKey("ConditionID")]
        public virtual MedicalCondition Condition { get; set; }
    }
}