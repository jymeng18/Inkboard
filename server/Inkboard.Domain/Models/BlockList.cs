using System.ComponentModel.DataAnnotations.Schema;

namespace Inkboard.Domain.Models;

[Table("Block_List")]
public class BlockList
{
  public Guid Id { get; set; }
  
  public Guid BlockedUserId { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  // nav props
  public User User { get; set; } = null!;
  
  public User BlockedUser { get; set; } = null!;
}