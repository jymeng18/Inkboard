using Inkboard.Domain.Models;

namespace Inkboard.Application.Interfaces;

public interface IPartyNotifier
{
    Task NotifyInvite(Guid userId, PartyInvite partyInvite);
    Task NotifyKick(Guid targetUserId, Guid partyId);
    Task NotifyLeadershipTransferred(Guid newLeaderId, Guid partyId);
    Task NotifyMemberJoined(Guid partyId, PartyMember newPartyMember);
    Task NotifyMemberLeft(Guid partyId, Guid userId);

    /* Both of these take the member ids explicitly rather than relying on the
     * hub group: a client that reconnected is no longer in its party group, and
     * these two events are the ones that must not be missed. */
    Task NotifyPartyEnded(Guid partyId, IReadOnlyCollection<Guid> memberIds);
    Task NotifyPartyCanvasOpened(
        Guid partyId,
        Guid canvasId,
        IReadOnlyCollection<Guid> memberIds
    );
}
