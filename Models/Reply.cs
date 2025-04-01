using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DotNetBackend.Models;

public class Reply
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string PostId { get; set; } // Foreign key to Post

    [ForeignKey("PostId")]
    public Post Post { get; set; }

    [Required]
    [MaxLength(200)]
    public string Text { get; set; }

    [Required]
    public string UserId { get; set; } // Foreign key to the user (could be just a string from JWT)

    [ForeignKey("UserId")]
    public User User { get; set; } //

    [Required]
    public string Username { get; set; } // Snapshot username

    public string? UserProfilePic { get; set; } // Snapshot profile picture

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; }
}
