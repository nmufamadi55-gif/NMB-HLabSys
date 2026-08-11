using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    public class TestResult
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public int RequestTestID { get; set; }

        public string? ResultValue { get; set; }
        public decimal? NumericValue { get; set; }
        public string? Notes { get; set; }
        public bool IsAbnormal { get; set; }
        public DateTime ResultDate { get; set; } = DateTime.Now;

        [ForeignKey("RequestTestID")]
        public virtual RequestTestType? RequestTestType { get; set; }

        public virtual ICollection<TestVerification>? Verifications { get; set; }
    }
}