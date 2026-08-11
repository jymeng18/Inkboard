#nullable enable
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkboard.Domain.Models;

[Table("Canvas_Operations")]
public class CanvasOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ActionType Type { get; set; }

    public string OperationData { get; set; } = ""; // * This is jsondata but BE never reads/writes/parse ret. JsonData, just broadcasts to other clients

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public Guid CanvasId { get; set; }

    // * which user performed this operation, null once that account is deleted:
    // * the stroke is kept for replay but is no longer attributed to anyone
    public Guid? UserId { get; set; }

    // *  Navigation props

    public Canvas Canvas { get; set; } = null!;

    public User? User { get; set; }
}

/// <summary>
/// Using tools like Ruler, or customizing brush size and thickness all counts as a
/// "Draw" operation,
/// </summary>
public enum ActionType
{
    Draw,
    Erase,
    Clear,
    Fill,
}
