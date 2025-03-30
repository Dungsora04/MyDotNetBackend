using System.Security.Claims;
using DotNetBackend.Data;
using DotNetBackend.Dtos;
using DotNetBackend.Models;
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
                    .Include(p => p.User)        // postedBy
                    .Include(p => p.Replies)     // include replies
                    .Include(p => p.Likes)      // Include PostLikes (if configured properly)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (post == null)
                {
                    return NotFound(new { message = "Post not found" });
                }

                return Ok(post);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GET POST BY ID ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "[GET POST BY ID] Error" });
            }
        }

        [HttpPost("delete/{id}")]
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

    }
}
