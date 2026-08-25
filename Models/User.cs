
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(20)]
        public string UserRole { get; set; } // Patient, Doctor, Technician, Manager, Admin

        public bool IsActive { get; set; } = true;

        public bool IsPasswordChanged { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual Doctor? Doctor { get; set; }
        public virtual LabTechnician? LabTechnician { get; set; }
        public virtual Patient? Patient { get; set; }
    }
}