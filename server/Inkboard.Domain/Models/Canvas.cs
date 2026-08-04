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

    [MaxLength(50)]
    public string? Name { get; set; }

    public string? SnapshotURL { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastModifiedAt { get; set; }

    // nav props
    public User Owner { get; set; } = null!;
}
