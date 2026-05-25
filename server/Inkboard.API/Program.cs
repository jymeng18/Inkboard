using Inkboard.Application;
using Inkboard.Infra;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("WebApiDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
  throw new InvalidOperationException("Connection string 'WebApiDatabase' not found.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseNpgsql(connectionString));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());

app.Run();
