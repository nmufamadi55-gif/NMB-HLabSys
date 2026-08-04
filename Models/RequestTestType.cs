using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class RequestTestType
    {
        [Key]
        public int RequestTestID { get; set; }

        [Required]
        public int RequestID { get; set; }

        [Required]
        public int TestTypeID { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Submitted";

        public int? AssignedTechnicianID { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }

        // Navigation properties
        [ForeignKey("RequestID")]
        public virtual TestRequest? TestRequest { get; set; }

        [ForeignKey("TestTypeID")]
        public virtual TestType? TestType { get; set; }

        public virtual ICollection<TestResult>? TestResults { get; set; }
    }
}