using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
  {
  }

  public DbSet<User> Users { get; set; }
  public DbSet<RefreshToken> RefreshTokens { get; set; }
} 
