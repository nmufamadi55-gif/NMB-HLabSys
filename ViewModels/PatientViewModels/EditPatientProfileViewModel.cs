using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class EditPatientProfileViewModel
    {
        public int PatientID { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [Phone]
        [StringLength(15)]
        public string Cellphone { get; set; }

        [StringLength(500)]
        public string HomeAddress { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
