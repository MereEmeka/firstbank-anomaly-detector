using System.ComponentModel.DataAnnotations;

namespace FirstBank.API.DTOs
{
    //This uses DTOs to encapsulate incoming request data
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
