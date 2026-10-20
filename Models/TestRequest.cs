using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class TestRequest
    {
        [Key]
        public int RequestID { get; set; }

        [Required]
        [StringLength(20)]
        public string RequestNumber { get; set; } = string.Empty;

        [Required]
        public int DoctorID { get; set; }

        [Required]
        public int PatientID { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Urgency { get; set; } = "routine"; // routine, urgent, stat

        public string? ClinicalNotes { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Submitted";

        public DateTime? CompletedDate { get; set; }
        public DateTime? ReleasedToPatientDate { get; set; }

        // ═══════════════════════════════════════════════════════════
        // DOCTOR RELEASE FIELDS
        // ═══════════════════════════════════════════════════════════
        [StringLength(500)]
        public string? ReleaseNote { get; set; }

        [StringLength(30)]
        public string? ReleaseMethod { get; set; }   // "email" or "appointment"

        public DateTime? AppointmentRequestedDate { get; set; }
        // ═══════════════════=═══════════════════════════════════════

        public DateTime? CancelledDate { get; set; }
        public string? CancellationReason { get; set; }

        // Navigation properties
        [ForeignKey("DoctorID")]
        public virtual Doctor? Doctor { get; set; }

        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }

        public virtual ICollection<ConsentTestRequestAccess>? ConsentTestRequestAccesses { get; set; }
        public virtual ICollection<RequestTestType>? RequestTestTypes { get; set; }
        public virtual ICollection<Sample>? Samples { get; set; }
    }
}