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
        public int DoctorID { get; set; } // References Doctors table

        public DateTime GrantedDate { get; set; } = DateTime.Now;

        public DateTime? RevokedDate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("PatientID")]
        public virtual Patient Patient { get; set; }

        [ForeignKey("DoctorID")]
        public virtual Doctor Doctor { get; set; }

        public virtual ICollection<ConsentTestRequest> ConsentTestRequests { get; set; }
    }

    public class ConsentTestRequest
    {
        [Key]
        public int ConsentTestRequestID { get; set; }

        [Required]
        public int ConsentID { get; set; }

        [Required]
        public int TestRequestID { get; set; } // References TestRequests table

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("ConsentID")]
        public virtual PatientConsentManagement Consent { get; set; }
    }
}
