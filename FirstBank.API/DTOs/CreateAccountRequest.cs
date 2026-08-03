using System.ComponentModel.DataAnnotations;

namespace FirstBank.API.DTOs
{
    public class CreateAccountRequest
    {
        [Range(0, double.MaxValue, ErrorMessage = "Initial balance cannot be negative.")]
        public decimal InitialBalance { get; set; }
        public string Currency { get; set; } = "NGN";

    }
}
