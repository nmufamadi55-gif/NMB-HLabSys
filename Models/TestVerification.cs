using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class TestVerification
    {
        [Key]
        public int VerificationID { get; set; }

        [Required]
        public int ResultID { get; set; }

        [Required]
        public int VerifiedByTechnicianID { get; set; }

        public DateTime VerificationDate { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string Status { get; set; } = "Verified"; // Verified, Returned

        public string? ReviewNotes { get; set; }

        [ForeignKey("ResultID")]
        public virtual TestResult? TestResult { get; set; }
    }
}