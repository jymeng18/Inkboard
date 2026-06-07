using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkboard.Domain.Models;

[Table("RefreshTokens")]
public class RefreshToken
{
  [Key]
  public Guid Id { get; set; } = Guid.NewGuid();
  
  public string TokenHash { get; set; }= "";

  public DateTime ExpiresAt { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public bool IsRevoked { get; set; }

  public Guid UserId { get; set; }

  [ForeignKey("UserId")]
  
  // Navigation property, UserId (FK) refers to User.Id (PK of Users)
  public User User { get; set; } = null!;

}