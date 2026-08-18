namespace Inkboard.Application.Canvases.DTO;

// * Record struct to avoid so many heap allocs, since this is getting binded many times
// * Record struct is a mutable value, type, add readonly for immut
public readonly record struct CursorPositionModel(Guid? UserId, int X, int Y)
{
    // No set defined Canvas dimension yet
    private const int MaxCanvasWidth = 100_000;
    private const int MaxCanvasHeight = 100_000;

    public bool IsValid()
    {
        return Math.Abs(Y) <= MaxCanvasHeight && Math.Abs(X) <= MaxCanvasWidth;
    }
}
