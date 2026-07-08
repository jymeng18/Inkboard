using Inkboard.Domain.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace Inkboard.API.Hubs;

public sealed class PartyHub : Hub<IPartyHubClient>
{
    private readonly IPartyRepository _partyRepository;

    public PartyHub(IPartyRepository partyRepository)
    {
        _partyRepository = partyRepository;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            Context.Abort();
            return;
        }

        var party = await _partyRepository.GetActivePartyForUserAsync(userId);
        if (party is null)
        {
            Context.Abort();
            return;
        }
        var groupName = PartyHub.GroupName(party.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Note: NotifyOnConnection(..) is a method that will be exposed to receive data from socket
        await Clients.OthersInGroup(groupName).NotifyOnConnection(userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdStr = Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            Context.Abort();
            return;
        }

        var party = await _partyRepository.GetActivePartyForUserAsync(userId);
        if (party is null)
        {
            Context.Abort();
            return;
        }
        var groupName = PartyHub.GroupName(party.Id);
        await Clients.OthersInGroup(groupName).NotifyOnDisconnect(userId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        await base.OnDisconnectedAsync(exception);
    }
    public static string GroupName(Guid partyId) => $"party-{partyId}";
}
