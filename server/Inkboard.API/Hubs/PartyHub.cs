using Inkboard.Application.Interfaces;
using Inkboard.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Inkboard.API.Hubs;

[Authorize]
public sealed class PartyHub : Hub<IPartyHubClient>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IConnectionStore _connectionStore;

    public PartyHub(IPartyRepository partyRepository, IConnectionStore connectionStore)
    {
        _partyRepository = partyRepository;
        _connectionStore = connectionStore;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            Context.Abort();
            return;
        }

        _connectionStore.Add(Context.ConnectionId, userId);

        /// <summary>
        // Re-join a (re)connecting member to their party's group, so group
        // broadcasts (member left, joined, kicked, leadership) reach them and
        // they show as online. Without this, a member who reloaded back into a
        // party is silently absent from the group and misses every broadcast.
        /// </summary>
        var party = await _partyRepository.GetActivePartyForUserAsync(userId);
        if (party is not null)
        {
            var groupName = PartyHub.GroupName(party.Id);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.OthersInGroup(groupName).NotifyOnConnection(userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdStr = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            Context.Abort();
            return;
        }
        _connectionStore.Remove(userId);

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
