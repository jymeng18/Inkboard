using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkboard.Domain.Models;

[Table("Canvases")]
public class Canvas
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Width { get; set; }

    public int Height { get; set; }

    public string SnapshotURL { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastModifiedAt { get; set; }

    // nav props

    /// <summary>
    /// Each active Party has an active Canvas mapped to it
    /// 1-to-1 relationship, upon Party being dissolved, Canvas closes, Canvas.PartyId should be set to null
    /// and when a user opens it back up, Canvas.PartyId should be set to the new PartyId
    /// </summary>
    // public Party Party { get; set; } = null!;
}
