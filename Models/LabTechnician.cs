using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class LabTechnician
    {
        [Key]
        public int TechnicianID { get; set; }

        [Required]
        public int UserID { get; set; }  // Changed from UserId to UserID

        [Required]
        [StringLength(50)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(13)]
        public string SAIDNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string Specialization { get; set; } = string.Empty;

        [StringLength(200)]
        public string Qualification { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }

        public DateTime HireDate { get; set; }

        public bool IsActive { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<TechnicianTestAssignment> TestAssignments { get; set; } = new List<TechnicianTestAssignment>();
    }
}