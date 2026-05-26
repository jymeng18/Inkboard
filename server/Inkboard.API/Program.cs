using Inkboard.Application;
using Inkboard.Infra;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Identity.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
  options.RequireHttpsMetadata = false;
  options.SaveToken = true;
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
    ValidAudience = builder.Configuration["JwtConfig:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtConfig:Jwt:Key"])),
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true, 
    ValidateIssuerSigningKey = true
  };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenGenerator>();


var connectionString = builder.Configuration.GetConnectionString("WebApiDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
  throw new InvalidOperationException("Connection string 'WebApiDatabase' not found.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseNpgsql(connectionString));


var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok()).RequireAuthorization();

app.MapPost("/login", (LoginRequest request, TokenGenerator tokenGenerator) =>
{

    var mockUserId = Guid.NewGuid();
    var token = tokenGenerator.GenerateToken(mockUserId, request.Email);
    return Results.Ok(new
    {
        access_token = token,
    });
});


app.Run();
