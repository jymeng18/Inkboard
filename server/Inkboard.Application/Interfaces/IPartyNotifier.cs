using Inkboard.Domain.Models;

namespace Inkboard.Application.Interfaces;

public interface IPartyNotifier
{
    Task NotifyInvite(Guid userId, PartyInvite partyInvite);
    Task NotifyKick(Guid targetUserId, Guid partyId);
    Task NotifyLeadershipTransferred(Guid newLeaderId, Guid partyId);
    Task NotifyMemberJoined(Guid partyId, PartyMember newPartyMember);
}
