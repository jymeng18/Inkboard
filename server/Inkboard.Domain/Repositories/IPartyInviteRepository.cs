using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories
{
    public interface IPartyInviteRepository
    {
        Task<PartyInvite?> GetByIdAsync(Guid inviteId);
        Task<PartyInvite?> GetPendingInviteAsync(Guid partyId, Guid invitedUserId);
        Task CreateInviteAsync(PartyInvite invite);
        Task UpdateInviteAsync(PartyInvite invite);
        Task SaveChangesAsync();
    }
}