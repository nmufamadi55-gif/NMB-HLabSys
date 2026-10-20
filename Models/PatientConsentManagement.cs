using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class PatientConsentManagement
    {
        [Key]
        public int ConsentID { get; set; }

        [Required]
        public int PatientID { get; set; }

        [Required]
        public int DoctorID { get; set; }

        public DateTime GrantedDate { get; set; } = DateTime.Now;

        public DateTime? RevokedDate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Notes { get; set; }   // ✅ now nullable

        // Navigation properties
        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }

        [ForeignKey("DoctorID")]
        public virtual Doctor? Doctor { get; set; }

        // ✅ Link to per-request accesses
        public virtual ICollection<ConsentTestRequestAccess>? ConsentTestRequestAccesses { get; set; }
    }
}