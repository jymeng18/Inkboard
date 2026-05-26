using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Inkboard.Domain;

[Table("Users")]
public class User
{
  [Key]
  public Guid Id { get; set; } = Guid.NewGuid();

  [Required]
  [MaxLength(30)]
  public string UserName { get; set; }

  [Required]
  [MaxLength(40)]
  public string Email { get; set; }

  [Required]
  public string PasswordHash { get; set; } 

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
