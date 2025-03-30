using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotNetBackend.Models
{

    public class PostLike
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N"); // Generate a new GUID for the Id

        [Required]
        public string PostId { get; set; }

        [ForeignKey("PostId")]
        public Post Post { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}