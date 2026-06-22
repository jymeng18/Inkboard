using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories
{
    // Combines Party and PartyMember Models
    public interface IPartyRepository
    {
        Task<Party?> GetByIdAsync(Guid partyId);
        Task<Party?> GetByLeaderIdAsync(Guid leaderId);
        Task<bool> IsUserInPartyAsync(Guid partyId, Guid userId);
        Task<int> GetMemberCountAsync(Guid partyId);
        Task<PartyMember?> GetMemberAsync(Guid partyId, Guid userId);

        // Note: might remove this, initial plan is to track this in case party leader leaves, 
        // where leader position transfers to oldest memeber in the party
        Task<PartyMember> GetOldestMemberAsync(Guid partyId);
        Task CreatePartyAsync(Party party);
        Task AddMemberAsync(PartyMember member);
        Task RemoveMemberAsync(PartyMember member);
        Task UpdatePartyAsync(Party party);
        Task DeletePartyAsync(Party party);
    }
}
