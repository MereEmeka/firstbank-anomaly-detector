using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstBank.Core.Models
{
    public class AnomalyLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }  //This tells EF Core to auto-incrementing int for log tables

        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        [MaxLength(500)]
        public string FlagReason { get; set; } = string.Empty;

        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    }
}
