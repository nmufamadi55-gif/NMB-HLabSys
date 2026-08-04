using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class Sample
    {
        [Key]
        public int SampleID { get; set; }

        [Required]
        public int RequestID { get; set; }

        [Required]
        public int SampleTypeID { get; set; }

        [Required]
        [StringLength(50)]
        public string Barcode { get; set; } = string.Empty;

        public DateTime? DateReceived { get; set; }
        public int? ReceivedByTechnicianID { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [ForeignKey("RequestID")]
        public virtual TestRequest? TestRequest { get; set; }

        [ForeignKey("SampleTypeID")]
        public virtual SampleType? SampleType { get; set; }
    }
}