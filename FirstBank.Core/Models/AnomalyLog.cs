using System;
using System.ComponentModel.DataAnnotations;

namespace FirstBank.Core.Models
{
    public class AnomalyLog
    {
        [Key]
        public Guid Id { get; set; }  //This uses an auto-incrementing int for log tables
        [Required]
        public Guid TransactionId { get; set; }
        [Required]
        [MaxLength(500)]
        public string FlagReason { get; set; } = string.Empty;
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    }
}
