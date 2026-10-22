using System;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.ViewModels.AdminViewModels
{
    public class AdminProfileViewModel
    {
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string ContactNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}