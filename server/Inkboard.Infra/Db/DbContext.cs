using Inkboard.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
  {
  }

  public DbSet<User> Users { get; set; }
} 
