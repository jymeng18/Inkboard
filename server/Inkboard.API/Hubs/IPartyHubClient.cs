using Inkboard.Domain.Models;

namespace Inkboard.API.Hubs;

/*
 * This interface has no implmentation, it is something that signalr exposes to the client side
*/
public interface IPartyHubClient
{
    Task NotifyOnConnection(Guid userId);
    Task NotifyOnDisconnect(Guid userId);
    Task NotifyOnMemberJoined(Guid userId);
    Task NotifyOnMemberLeft(Guid userId);
    Task NotifyOnKick(Guid userId);
    Task ReceiveInvite(PartyInvite partyInvite);
    Task LeadershipTransferred(Guid newLeaderId);
}
