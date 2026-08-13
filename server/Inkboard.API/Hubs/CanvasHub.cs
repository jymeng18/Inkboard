using Inkboard.Application.Canvases.DTO;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Inkboard.API.Hubs;

[Authorize]
public class CanvasHub : Hub<ICanvasHubClient>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IConnectionStore _connectionStore;
    private readonly ICanvasNotifier _canvasNotifier;
    public static readonly string HubName = "CanvasHub";

    public CanvasHub(
        IPartyRepository partyRepository,
        IConnectionStore connectionStore,
        ICanvasNotifier canvasNotifier
    )
    {
        _partyRepository = partyRepository;
        _connectionStore = connectionStore;
        _canvasNotifier = canvasNotifier;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.UserIdentifier;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            Context.Abort();
            return;
        }

        _connectionStore.Add(Context.ConnectionId, userId, HubName);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdStr = Context.UserIdentifier;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            Context.Abort();
            return;
        }

        _connectionStore.Remove(userId, HubName);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// This method should be invoked after:
    /// 1. A solo user decides to invite someone, they themselves need to join too after it adds them into party
    /// 2. An invited user accepts a PartyInvite, after the system adds them to the Party
    /// </summary>
    public async Task JoinCanvas(Guid canvasId)
    {
        var userIdStr = Context.UserIdentifier;
        if (!Guid.TryParse(userIdStr, out var userId))
            throw new HubException("Unauthorized.");

        var party = await _partyRepository.GetActivePartyForUserAsync(userId);
        if (party?.CanvasId is null || party.CanvasId != canvasId)
            throw new HubException("You don't have an active session on this canvas.");

        Guid? oldCanvasId = null;
        bool switchedCanvas = false;

        // * Check if the user switched a Canvas session,
        if (
            Context.Items.TryGetValue("CanvasId", out var oldCachedCanvasId)
            && oldCachedCanvasId is not null
        )
        {
            oldCanvasId = (Guid)oldCachedCanvasId;
            if (oldCanvasId != canvasId)
            {
                switchedCanvas = true;
            }
        }

        // Cache each Canvas sessions ID into dictionary
        Context.Items["CanvasId"] = party.CanvasId.Value;

        // If party switches Canvas sessions, ID changes, must update & leave group with old Canvas ID
        if (switchedCanvas && oldCanvasId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(oldCanvasId));

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(canvasId));
    }

    public async Task SendCursorPos(CursorPositionModel cursor)
    {
        var userIdStr = Context.UserIdentifier;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return;
        }

        if (!cursor.IsValid())
            return;

        if (!Context.Items.TryGetValue("CanvasId", out var cachedCanvasId))
            return;

        var canvasId = (Guid)cachedCanvasId!;

        var stamped = cursor with { UserId = userId };

        await _canvasNotifier.NotifyCursorPos(stamped, canvasId, Context.ConnectionId);
    }

    public async Task SendOperation(CanvasOperation operation)
    {
        // TODO: LEAVE FOR NOW
    }

    public static string GroupName(Guid? canvasId) => $"canvas-{canvasId}";
}
