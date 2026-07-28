using System;
using System.Collections.Generic;
using System.Text;

namespace FirstBank.API.DTOs
{
    public class CreateTransactionRequest
    {
        public string SourceAccountId { get; set; } = string.Empty;
        public string DestinationAccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}