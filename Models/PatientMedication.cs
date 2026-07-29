using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class PatientMedication
    {
        [Key]
        public int PatientMedicationID { get; set; }

        [Required]
        public int PatientID { get; set; }

        [Required]
        public int MedicationID { get; set; }

        [StringLength(50)]
        public string Dosage { get; set; }

        [StringLength(50)]
        public string Frequency { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsCurrent { get; set; } = true;

        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("PatientID")]
        public virtual Patient Patient { get; set; }

        [ForeignKey("MedicationID")]
        public virtual Medication Medication { get; set; }
    }
}