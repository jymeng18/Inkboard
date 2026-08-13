using Inkboard.API.Hubs;
using Inkboard.Application.Canvases.DTO;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace Inkboard.API.Realtime;

public class CanvasNotifier : ICanvasNotifier
{
    private readonly IHubContext<CanvasHub, ICanvasHubClient> _hub;
    private readonly IConnectionStore _connectionStore;

    public CanvasNotifier(
        IHubContext<CanvasHub, ICanvasHubClient> hub,
        IConnectionStore connectionStore
    )
    {
        _hub = hub;
        _connectionStore = connectionStore;
    }

    public async Task NotifyCursorPos(
        CursorPositionModel cursorPosition,
        Guid canvasId,
        string connId
    )
    {
        var groupName = CanvasHub.GroupName(canvasId);
        await _hub.Clients.GroupExcept(groupName, connId).NotifyOnCursorPos(cursorPosition);
    }

    public async Task NotifyOperation(CanvasOperation canvasOperation, Guid canvasId, string connId)
    {
        var groupName = CanvasHub.GroupName(canvasId);
        await _hub.Clients.GroupExcept(groupName, connId).NotifyOnOperation(canvasOperation);
    }
}
