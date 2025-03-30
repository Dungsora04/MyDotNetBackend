using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotNetBackend.Models
{
    public class UserFollow
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N"); // Generate a new GUID for the Id

        [ForeignKey("Follower")]
        public string FollowerId { get; set; }
        public User Follower { get; set; }

        [ForeignKey("Following")]
        public string FollowingId { get; set; }
        public User Following { get; set; }
    }
}