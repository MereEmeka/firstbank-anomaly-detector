using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text;

namespace FirstBank.Core.Models
{
    public class Account
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "NGN";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}