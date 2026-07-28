namespace FirstBank.API.DTOs
{
    public class CreateAccountRequest
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; } 
        public string Currency { get; set; } = "NGN";

    }
}
