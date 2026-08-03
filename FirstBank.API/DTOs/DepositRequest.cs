using System.ComponentModel.DataAnnotations;

namespace FirstBank.API.DTOs
{
    public class DepositRequest
    {
        [Required]
        [Range(1.00, double.MaxValue, ErrorMessage ="Deposit amount must be higher than zero.")]
        public decimal Amount { get; set; }
    }
}
