using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class PatientAllergy
    {
        [Key]
        public int PatientAllergyID { get; set; }

        [Required]
        public int PatientID { get; set; }

        [Required]
        public int AllergyID { get; set; }

        [StringLength(20)]
        public string Severity { get; set; } // Mild, Moderate, Severe

        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("PatientID")]
        public virtual Patient Patient { get; set; }

        [ForeignKey("AllergyID")]
        public virtual Allergy Allergy { get; set; }
    }
}