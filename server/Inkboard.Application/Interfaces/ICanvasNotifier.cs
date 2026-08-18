using Inkboard.Application.Canvases.DTO;
using Inkboard.Domain.Models;

namespace Inkboard.Application.Interfaces;

public interface ICanvasNotifier
{
    Task NotifyCursorPos(CursorPositionModel cursorPosition, Guid canvasId, string connId);
    Task NotifyOperation(CanvasOperation canvasOperation, Guid canvasId, string connId);
    Task NotifyLiveStroke(LiveStrokeModel liveStroke, Guid canvasId, string connId);
}
