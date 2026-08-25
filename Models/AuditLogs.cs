using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NMB_HLabSys.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public int AuditLogID { get; set; }

        [Required]
        [StringLength(100)]
        public string ActivityType { get; set; } = string.Empty; // e.g., "Stock Adjustment", "Doctor Provisioned", "Order Cancelled"

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime ActivityDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(256)]
        public string UserName { get; set; } = string.Empty; // Username/email of the user who performed the action
        public int? UserID { get; set; }

        [StringLength(50)]
        public string? IPAddress { get; set; }
    }
}