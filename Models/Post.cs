using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotNetBackend.Models
{
    public class Post
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Required]
        public string PostedById { get; set; } // UserId (string for JWT users)

        [ForeignKey("PostedById")]
        public User User { get; set; }

        [Required]
        [MaxLength(500)]
        public string Text { get; set; }

        public string? Img { get; set; }

        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();

        public ICollection<Reply> Replies { get; set; } = new List<Reply>();

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime UpdatedAt { get; set; }
    }
}