using System.ComponentModel.DataAnnotations;

namespace DotNetBackend.DTOs
{
    public class PostReplyDto
    {
        [Required]
        [MaxLength(200)]
        public string Text { get; set; }
    }
}
