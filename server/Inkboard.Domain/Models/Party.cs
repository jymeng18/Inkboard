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

    public Guid? CanvasId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation props
    public User Leader { get; set; } = null!;

    /// <summary>
    /// Each active party has its Canvas, if a user closes and opens multiple parties all 
    /// on the same Canvas, this forms an N-to-1 relationship
    /// </summary>
    public Canvas Canvas { get; set; } = null!;
}
