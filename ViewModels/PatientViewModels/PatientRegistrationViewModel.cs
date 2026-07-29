using System;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientRegistrationViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [StringLength(13)]
        [MinLength(13)]
        [RegularExpression(@"^[0-9]{13}$", ErrorMessage = "SA ID must be 13 digits")]
        public string SAIDNumber { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Phone]
        [StringLength(15)]
        public string Cellphone { get; set; }

        [StringLength(500)]
        public string HomeAddress { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must have at least 8 characters, one uppercase letter, one number, and one special character")]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
