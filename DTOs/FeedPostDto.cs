using System.ComponentModel.DataAnnotations;

namespace DotNetBackend.Dtos
{
    public class FeedPostDto
    {
        public string Id { get; set; }
        public string PostedById { get; set; }
        public string PostedByUsername { get; set; }
        public string? PostedByProfilePic { get; set; }
        public string Text { get; set; }
        public string? Img { get; set; }
        public List<FeedReplyDto> Replies { get; set; }
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FeedReplyDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string? UserProfilePic { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}