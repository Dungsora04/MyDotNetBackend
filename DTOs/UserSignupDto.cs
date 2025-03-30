using System.ComponentModel.DataAnnotations;

namespace DotNetBackend.DTOs
{
    public class UserSignupDto 
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Username { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }
}