using Inkboard.API.Hubs;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace Inkboard.API.Realtime;

public class PartyNotifier : IPartyNotifier
{
    /*
        * Note: Auth system maps user guid in the claim, SignalR maps ConnectionId
        * from socket to UserIdentifier In memory lookup for active sockets on the guid
    */

    private readonly IHubContext<PartyHub, IPartyHubClient> _hub;
    private readonly IConnectionStore _connectionStore;

    public PartyNotifier(IHubContext<PartyHub, IPartyHubClient> hub, IConnectionStore connectionStore)
    {
        _hub = hub;
        _connectionStore = connectionStore;
    }

    public Task NotifyInvite(Guid userId, PartyInvite partyInvite)
    {
        return _hub.Clients.User(userId.ToString()).ReceiveInvite(partyInvite);
    }

    public async Task NotifyKick(Guid targetUserId, Guid partyId)
    {
        var connId = _connectionStore.Get(targetUserId, PartyHub.HubName);
        var groupName = PartyHub.GroupName(partyId);
        if (connId is not null)
        {
            await _hub.Groups.RemoveFromGroupAsync(connId, groupName);
        }

        await _hub.Clients.User(targetUserId.ToString()).NotifyOnKick(targetUserId);
        await _hub.Clients.Group(groupName).NotifyOnKick(targetUserId);
    }

    public Task NotifyLeadershipTransferred(Guid newLeaderId, Guid partyId)
    {
        return _hub.Clients.Group(PartyHub.GroupName(partyId)).LeadershipTransferred(newLeaderId);
    }

    public async Task NotifyMemberJoined(Guid partyId, PartyMember newMember)
    {
        var connId = _connectionStore.Get(newMember.UserId, PartyHub.HubName);
        var groupName = PartyHub.GroupName(partyId);
        if (connId is not null)
        {
            await _hub.Groups.AddToGroupAsync(connId, groupName);
            await _hub.Clients.GroupExcept(groupName, connId).NotifyOnMemberJoined(newMember.UserId);
        }
    }

    public async Task NotifyMemberLeft(Guid partyId, Guid userId)
    {
        var connId = _connectionStore.Get(userId, PartyHub.HubName);
        var groupName = PartyHub.GroupName(partyId);

        if (connId is not null)
        {
            await _hub.Groups.RemoveFromGroupAsync(connId, groupName);
            await _hub.Clients.Group(groupName).NotifyOnMemberLeft(userId);
        }
    }

    public async Task NotifyPartyEnded(Guid partyId, IReadOnlyCollection<Guid> memberIds)
    {
        var groupName = PartyHub.GroupName(partyId);

        foreach (var memberId in memberIds)
        {
            await _hub.Clients.User(memberId.ToString()).PartyEnded();

            var connId = _connectionStore.Get(memberId, PartyHub.HubName);
            if (connId is not null)
                await _hub.Groups.RemoveFromGroupAsync(connId, groupName);
        }
    }

    public async Task NotifyPartyCanvasOpened(
        Guid partyId,
        Guid canvasId,
        IReadOnlyCollection<Guid> memberIds
    )
    {
        foreach (var memberId in memberIds)
        {
            await _hub.Clients.User(memberId.ToString()).PartyCanvasOpened(canvasId);
        }
    }
}
