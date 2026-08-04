using System;
using System.Collections.Generic;
using System.Text;

namespace FirstBank.API.DTOs
{
    public class CreateTransactionRequest
    {
        public Guid SourceAccountId { get; set; } 
        public Guid DestinationAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}