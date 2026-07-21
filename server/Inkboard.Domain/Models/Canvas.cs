#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkboard.Domain.Models;

[Table("Canvases")]
public class Canvas
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }

    public string? Name { get; set; }

    public string? SnapshotURL { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastModifiedAt { get; set; }

    // nav props
    public User Owner { get; set; } = null!;
}
