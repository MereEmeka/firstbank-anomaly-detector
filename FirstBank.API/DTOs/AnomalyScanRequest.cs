using System;
using System.ComponentModel.DataAnnotations;

namespace FirstBank.API.DTOs
{
    public class AnomalyScanRequest
    {
        [Required]
        public Guid AccountId { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal TransactionAmount { get; set; }

        public string Location { get; set; } = "Lagos, Nigeria";
    }
}
