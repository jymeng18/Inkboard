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

    public PartyNotifier(IHubContext<PartyHub, IPartyHubClient> hub)
    {
        _hub = hub;
    }

    public Task NotifyInvite(Guid userId, PartyInvite partyInvite)
    {
        return _hub.Clients.User(userId.ToString()).ReceiveInvite(partyInvite);
    }

    public Task NotifyKick(Guid targetUserId, Guid partyId)
    {
        return _hub.Clients.User(targetUserId.ToString()).NotifyOnKick(partyId);
    }

    public Task NotifyLeadershipTransferred(Guid newLeaderId, Guid partyId)
    {
        return _hub.Clients.Group(PartyHub.GroupName(partyId)).LeadershipTransferred(newLeaderId);
    }

    public Task NotifyMemberJoined(Guid partyId, PartyMember newMember)
    {
        return _hub.Clients.Group(PartyHub.GroupName(partyId)).NotifyOnMemberJoined(newMember.UserId);
    }
}
