using System;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [MaxLength(200)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UsedAt { get; set; }

        // Navigation
        public User? User { get; set; }
    }
}