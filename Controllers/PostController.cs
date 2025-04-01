using System.Security.Claims;
using DotNetBackend.Data;
using DotNetBackend.Dtos;
using DotNetBackend.DTOs;
using DotNetBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotNetBackend.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PostController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePost([FromBody] PostCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not authenticated! Please log in" });
                }

                if (dto.Text.Length > 500)
                {
                    return BadRequest(new { message = "Text must be less than or equal to 500 characters." });
                }

                var newPost = new Post
                {
                    Id = Guid.NewGuid().ToString("N"),
                    PostedById = currentUser.Id,
                    Text = dto.Text,
                    Img = dto.Img,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Posts.Add(newPost);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(CreatePost), new { id = newPost.Id }, new
                {
                    message = "Post created successfully",
                    post = new
                    {
                        newPost.Id,
                        newPost.Text,
                        newPost.Img,
                        newPost.Likes,
                        newPost.CreatedAt,
                        newPost.UpdatedAt,
                        PostedBy = new
                        {
                            currentUser.Id,
                            currentUser.Username,
                            currentUser.ProfilePic
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CREATE ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while creating the post." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostById(string id)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Replies)
                    .Include(p => p.Likes)
                        .ThenInclude(like => like.User)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (post == null)
                {
                    return NotFound(new { message = "Post not found" });
                }

                // Build the response manually to avoid circular refs
                var response = new
                {
                    post.Id,
                    post.Text,
                    post.Img,
                    post.CreatedAt,
                    post.UpdatedAt,
                    PostedBy = new
                    {
                        post.User.Id,
                        post.User.Username,
                        post.User.ProfilePic
                    },
                    Likes = post.Likes.Select(like => new
                    {
                        like.User.Id,
                        like.User.Username,
                        like.User.ProfilePic
                    }),
                    Replies = post.Replies.Select(reply => new
                    {
                        reply.Id,
                        reply.Text,
                        reply.CreatedAt,
                        reply.UserId,
                        reply.Username,
                        reply.UserProfilePic
                    })
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GET POST BY ID ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "[GET POST BY ID] Error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(string id)
        {
            try
            {
                var currentUser = HttpContext.Items["User"] as User;

                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not authenticated! Please log in" });
                }

                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                {
                    return NotFound(new { message = "Post not found" });
                }

                if (post.PostedById != currentUser.Id)
                {
                    return Unauthorized(new { message = "User not authorized to delete this post" });
                }

                // Delete related likes and replies
                var relatedLikes = _context.PostLikes.Where(l => l.PostId == id);
                _context.PostLikes.RemoveRange(relatedLikes);

                var relatedReplies = _context.Replies.Where(r => r.PostId == id);
                _context.Replies.RemoveRange(relatedReplies);

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Post deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DELETE POST ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while deleting the post." });
            }
        }

        [HttpPost("like/{id}")]
        public async Task<IActionResult> LikeUnlikePost(string id)
        {
            try
            {
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not authenticated! Please log in" });
                }

                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                {
                    return NotFound(new { message = "Post not found." });
                }

                // Check if like already exists
                var existingLike = await _context.PostLikes
                    .FirstOrDefaultAsync(l => l.PostId == id && l.UserId == currentUser.Id);

                if (existingLike != null)
                {
                    // Unlike
                    _context.PostLikes.Remove(existingLike);
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Post unliked successfully." });
                }

                // Like
                var newLike = new PostLike
                {
                    Id = Guid.NewGuid().ToString("N"),
                    PostId = id,
                    UserId = currentUser.Id
                };

                _context.PostLikes.Add(newLike);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Post liked successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LIKE/UNLIKE ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while liking/unliking the post." });
            }
        }

        [HttpPost("reply/{id}")]
        public async Task<IActionResult> ReplyToPost(string id, [FromBody] PostReplyDto replyDto)
        {
            try
            {
                 var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not authenticated! Please log in" });
                }

                if (string.IsNullOrWhiteSpace(replyDto.Text))
                {
                    return BadRequest(new { message = "Text field is required" });
                }

                var post = await _context.Posts.Include(p => p.Replies).FirstOrDefaultAsync(p => p.Id == id);
                if (post == null)
                {
                    return NotFound(new { message = "Post not found" });
                }

                var newReply = new Reply
                {
                    Id = Guid.NewGuid().ToString("N"),
                    PostId = id,
                    UserId = currentUser.Id,
                    Username = currentUser.Username,
                    UserProfilePic = currentUser.ProfilePic,
                    Text = replyDto.Text,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                post.Replies.Add(newReply);
                await _context.SaveChangesAsync();

                return Created("", new { message = "Reply added successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REPLY TO POST ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "[REPLY TO POST] Error" });
            }
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeedPosts()
        {
            try
            {
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not authenticated! Please log in" });
                }

                // Get the IDs of users the current user is following
                var followingIds = _context.UserFollows
                    .Where(f => f.FollowerId == currentUser.Id)
                    .Select(f => f.FollowingId)
                    .ToList();

                var feedPosts = await _context.Posts
                    .Where(p => followingIds.Contains(p.PostedById))
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new FeedPostDto
                    {
                        Id = p.Id,
                        PostedById = p.PostedById,
                        PostedByUsername = p.User.Username,
                        PostedByProfilePic = p.User.ProfilePic,
                        Text = p.Text,
                        Img = p.Img,
                        LikeCount = p.Likes.Count,
                        CreatedAt = p.CreatedAt,
                        Replies = p.Replies.Select(r => new FeedReplyDto
                        {
                            Id = r.Id,
                            UserId = r.UserId,
                            Username = r.Username,
                            UserProfilePic = r.UserProfilePic,
                            Text = r.Text,
                            CreatedAt = r.CreatedAt
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(feedPosts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GET FEED POSTS ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "[GET FEED POSTS] Error" });
            }
        }

    }
}
