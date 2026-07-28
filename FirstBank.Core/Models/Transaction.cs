using System;
using System.ComponentModel.DataAnnotations;

namespace FirstBank.Core.Models
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid SourceAccountId { get; set; }
        [Required]
        public Guid DestinationAccountId { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        //This maps directly to the logic in the controller
        public bool IsAnomalyFlagged { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Completed"; // Possible values: "Completed", "Failed", "Pending"

    }
}
