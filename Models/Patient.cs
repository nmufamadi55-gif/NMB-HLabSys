using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class Patient
    {
        [Key]
        public int PatientID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(13)]
        [MinLength(13)]
        [RegularExpression(@"^[0-9]{13}$", ErrorMessage = "SA ID must be 13 digits")]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [StringLength(15)]
        public string Cellphone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        public string HomeAddress { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<PatientCondition> PatientConditions { get; set; } = new List<PatientCondition>();
        public virtual ICollection<PatientAllergy> PatientAllergies { get; set; } = new List<PatientAllergy>();
        public virtual ICollection<PatientMedication> PatientMedications { get; set; } = new List<PatientMedication>();
        public virtual ICollection<PatientConsentManagement> PatientConsents { get; set; } = new List<PatientConsentManagement>();
        public virtual ICollection<TestRequest>? TestRequests { get; set; }
    }
}