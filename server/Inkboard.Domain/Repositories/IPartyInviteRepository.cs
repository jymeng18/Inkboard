#nullable enable

using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories
{
    public interface IPartyInviteRepository
    {
        Task<PartyInvite?> GetByIdAsync(Guid inviteId);
        Task<PartyInvite?> GetPendingInviteAsync(Guid partyId, Guid invitedUserId);
        Task CreateInviteAsync(PartyInvite invite);
        Task UpdateInviteAsync(PartyInvite invite);
    }
}