using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using DotNetBackend.Data;
using Microsoft.EntityFrameworkCore;

public class RequireAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public RequireAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }


    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        var path = context.Request.Path.Value?.ToLower();

        // type here endpoints that do not require authentication
        if (path == "/api/users/login" || 
            path == "/api/users/logout" || 
            path == "/api/users/signup" || 
            path.StartsWith("/api/posts/") && context.Request.Method == "GET")
            {
                await _next(context);
                return;
            }

            var token = context.Request.Cookies["jwt-cookie"]; // Get token from cookie

            // Check if token is present
            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Unauthorized - token emptied - please log in" });
                return;
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(c => c.Type == "userId").Value;

                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new { message = "User not found" });
                    return;
                }

                // Attach user to HttpContext
                context.Items["User"] = user;
            }
            catch
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid token" });
                return;
            }

            await _next(context);
        }
    }
