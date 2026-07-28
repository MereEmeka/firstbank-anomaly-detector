using System;
using System.ComponentModel.DataAnnotations;

namespace FirstBank.Core.Models
{
    public class IdempotencyRecord
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
