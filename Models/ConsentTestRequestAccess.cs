using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class ConsentTestRequestAccess
    {
        [Key]
        public int ConsentTestRequestAccessID { get; set; }

        [Required]
        public int ConsentID { get; set; }

        [Required]
        public int RequestID { get; set; }

        [ForeignKey("ConsentID")]
        public virtual PatientConsent? PatientConsent { get; set; }

        [ForeignKey("RequestID")]
        public virtual TestRequest? TestRequest { get; set; }
    }
}