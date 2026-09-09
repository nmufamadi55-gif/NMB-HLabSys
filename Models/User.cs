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
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string UserRole { get; set; } = string.Empty;

        [StringLength(20)]
        public string ContactNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsPasswordChanged { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }
        public virtual LabTechnician? LabTechnician { get; set; }
        public string? AspNetUserID { get; set; }
    }
}