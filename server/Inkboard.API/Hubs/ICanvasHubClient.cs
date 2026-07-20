using Inkboard.Application.Canvases.DTO;
using Inkboard.Domain.Models;

namespace Inkboard.API.Hubs;

/*
 * This interface has no implmentation, it is something that signalr exposes to the client side
*/
public interface ICanvasHubClient
{
    Task NotifyOnOperation(CanvasOperation canvasOperation);

    Task NotifyOnCursorPos(CursorPositionModel cursorPosition);

    Task ReceiveCanvasSnapshot(string snapshotUrl);
}
