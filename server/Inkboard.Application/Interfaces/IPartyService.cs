using Inkboard.Domain.Models;

namespace Inkboard.Application.Interfaces
{
    public interface IPartyService
    {
        Task<Party> CreatePartyAsync(Guid leaderId);
        Task<PartyInvite> InviteUserAsync(Guid partyId, Guid leaderId, Guid invitedUserId);
        Task<PartyInvite> RespondToUserInviteAsync(Guid id, Guid userId, bool accepted);
        Task RemoveMemberAsync(Guid partyId, Guid leaderId, Guid targetUserId);
        Task LeavePartyAsync(Guid partyId, Guid userId);
        Task BlockUserAsync(Guid leaderId, Guid targetUserId);
    }
}
