using System;
using System.ComponentModel.DataAnnotations;

namespace FirstBank.Core.Models
{
    public class AppUser
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; //It could be "Admin" or "User"
    }
}