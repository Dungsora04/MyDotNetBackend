using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DotNetBackend.Services
{
    public class TokenServices
    {
        private readonly IConfiguration _config;

        public TokenServices(IConfiguration config)
        {
            _config = config;
        }

        public void GenerateTokenAndSetCookie(HttpResponse response, string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Secret"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim("userId", userId)
                }),
                Expires = DateTime.UtcNow.AddDays(15),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            response.Cookies.Append("jwt-cookie", jwt, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(15)
            });
        }
    }
}
