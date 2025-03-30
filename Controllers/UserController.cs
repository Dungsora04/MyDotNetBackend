using System.Security.Cryptography;
using System.Text;
using DotNetBackend.Data;
using DotNetBackend.DTOs;
using DotNetBackend.Models;
using DotNetBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotNetBackend.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenServices _tokenServices;

        public UserController(AppDbContext context, TokenServices tokenServices)
        {
            _context = context;
            _tokenServices = tokenServices;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup(UserSignupDto dto)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Email);
                if (existingUser != null)
                    return BadRequest(new { message = "User already exists" });

                // Hash the password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new User
                {
                    Name = dto.Name,
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = passwordHash
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _tokenServices.GenerateTokenAndSetCookie(Response, user.Id);

                return Created("", new
                {
                    user.Id,
                    user.Name,
                    user.Username,
                    user.Email
                });
            }
            catch (Exception ex)
            {
                // Log the exception if you want
                Console.WriteLine($"Signup Error: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid username or password" });
                }

                bool isPasswordMatch = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
                if (!isPasswordMatch)
                {
                    return Unauthorized(new { message = "Invalid username or password" });
                }

                _tokenServices.GenerateTokenAndSetCookie(Response, user.Id);

                return Ok(new
                {
                    user.Id,
                    user.Name,
                    user.Username,
                    user.Email
                    // DO NOT return the password hash
                });
            }
            catch (Exception ex)
            {
                // Log the exception if you want
                Console.WriteLine($"Login Error: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Append("jwt-cookie", "", new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(-1) // Expire the cookie immediately
            });
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("follow/{id}")]
        public async Task<IActionResult> FollowUnfollowUser(string id)
        {
            try
            {
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not authenticated! Please log in" });
                }

                if (id == currentUser.Id)
                {
                    return BadRequest(new { message = "You cannot follow/unfollow yourself" });
                }

                var userToFollow = await _context.Users.FindAsync(id);
                if (userToFollow == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var existingFollow = await _context.UserFollows.FirstOrDefaultAsync(f =>
                    f.FollowerId == currentUser.Id && f.FollowingId == id);

                if (existingFollow != null)
                {
                    // Unfollow
                    _context.UserFollows.Remove(existingFollow);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Unfollowed successfully" });
                }
                else
                {
                    // Follow
                    var newFollow = new UserFollow
                    {
                        FollowerId = currentUser.Id,
                        FollowingId = id
                    };

                    await _context.UserFollows.AddAsync(newFollow);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Followed successfully" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FOLLOW ERROR]: " + ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateUser(string id, UserUpdateDto dto)
        {
            try
            {
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not authenticated! Please log in" });
                }

                if (currentUser.Id != id)
                {
                    return StatusCode(403, new { message = "You are not authorized to update this user." });
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Hash new password if provided
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim());
                }

                // Apply updates with optional trimming
                user.Name = string.IsNullOrWhiteSpace(dto.Name) ? user.Name : dto.Name.Trim();
                user.Username = string.IsNullOrWhiteSpace(dto.Username) ? user.Username : dto.Username.Trim();
                user.Email = string.IsNullOrWhiteSpace(dto.Email) ? user.Email : dto.Email.Trim();
                user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? user.Bio : dto.Bio.Trim();
                user.ProfilePic = string.IsNullOrWhiteSpace(dto.ProfilePic) ? user.ProfilePic : dto.ProfilePic.Trim();

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    user.Id,
                    user.Name,
                    user.Username,
                    user.Email,
                    user.Bio,
                    user.ProfilePic
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UPDATE ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while updating the user." });
            }
        }

        [HttpGet("profile/{username}")]
        public async Task<IActionResult> GetUserProfile(string username)
        {
            try
            {
                var currentUser = HttpContext.Items["User"] as User; 
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "User not authenticated! Please log in" }); 
                }

                var user = await _context.Users
                    .Where(u => u.Username == username)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.Username,
                        u.Email,
                        u.Bio,
                        u.ProfilePic,
                        u.CreatedAt
                        // Exclude PasswordHash and UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GET PROFILE ERROR]: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

    }
}