using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class PatientConsent
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
        public virtual PatientConsent Consent { get; set; }
    }
}
