using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstBank.Core.Models
{ 
    // This isolates the table into an 'atm' schema in SQL Server
    [Table("Cards", Schema = "atm")]
    public class Card
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(16)]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        public string PinHash { get; set; } = string.Empty;
        public int FailedAttempts { get; set; } = 0;
        public bool IsBlocked { get; set; } = false;

        //Soft Link to the Core Banking Domain. No tightly couples Navigation Properties
        [Required]
        public Guid AccountId { get; set; }
    }
}
