using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class Doctor
    {
        [Key]
        public int DoctorID { get; set; }

        [Required]
        public int UserID { get; set; }  // Changed from UserId to UserID

        [Required]
        [StringLength(20)]
        public string HPCSANumber { get; set; } = string.Empty;

        [StringLength(200)]
        public string PracticeName { get; set; } = string.Empty;

        public string PracticeAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string Specialization { get; set; } = string.Empty;

        public bool IsVerified { get; set; }

        public DateTime RegisteredDate { get; set; }

        // Navigation property
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;
        public virtual ICollection<TestRequest> TestRequests { get; set; } = new List<TestRequest>();
        public virtual ICollection<PatientConsentManagement> PatientConsents { get; set; } = new List<PatientConsentManagement>();
    }
}