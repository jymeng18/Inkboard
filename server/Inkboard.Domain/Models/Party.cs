using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkboard.Domain.Models;

[Table("Parties")]
public class Party
{ 
  [Key]
  public Guid Id { get; set; } = Guid.NewGuid();

  // Each party has only one leader (1-to-1)
  public Guid LeaderId { get; set; }

  // keep as nullable for now, since no CanvasHub layer yet
  public Guid? CanvasId { get; set; }

  public DateTime CreatedAt { get; set; }


  // Navigation props
  public User Leader { get; set; } = null!;

}