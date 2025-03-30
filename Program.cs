using System.Text;
using DotNetBackend.Data;
using DotNetBackend.Extensions;
using DotNetBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args); // Create a new instance of the WebApplicationBuilder class.


builder.Services.AddControllers(); // Add a scoped service for the controllers.
builder.Services.AddJwtAuthentication(builder.Configuration); // Add the Jwt authentication service.
builder.Services.AddScoped<TokenServices>();
builder.Services.AddDbContext<AppDbContext>(option =>
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // Add a scoped service for the AppDbContext class.


var app = builder.Build();  // Build the app.

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RequireAuthMiddleware>();

app.MapControllers();

app.Run();

