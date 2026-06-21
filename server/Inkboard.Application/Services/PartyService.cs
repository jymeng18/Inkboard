using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;

namespace Inkboard.Application.Services
{
    public class PartyService : IPartyService
    {
        public Task BlockUserAsync(Guid leaderId, Guid targetUserId)
        {
            throw new NotImplementedException();
        }

        public Task<Party> CreatePartyAsync(Guid leaderId)
        {
            throw new NotImplementedException();
        }

        public Task<PartyInvite> InviteUserAsync(Guid partyId, Guid leaderId, Guid invitedUserId)
        {
            throw new NotImplementedException();
        }

        public Task LeavePartyAsync(Guid partyId, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveMemberAsync(Guid partyId, Guid leaderId, Guid targetUserId)
        {
            throw new NotImplementedException();
        }

        public Task<PartyInvite> RespondToUserInviteAsync(Guid id, Guid userId, bool accepted)
        {
            throw new NotImplementedException();
        }
    }
}
