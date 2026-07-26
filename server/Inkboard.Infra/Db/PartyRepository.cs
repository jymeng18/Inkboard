using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db
{
    public class PartyRepository : IPartyRepository
    {
        private readonly AppDbContext _context;

        public PartyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddMemberAsync(PartyMember member)
        {
            _context.PartyMembers.Add(member);
            await _context.SaveChangesAsync();
        }

        public async Task CreatePartyAsync(Party party)
        {
            _context.Parties.Add(party);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePartyAsync(Party party)
        {
            _context.Parties.Remove(party);
            await _context.SaveChangesAsync();
        }

        public Task<Party?> GetActivePartyForUserAsync(Guid userId)
        {
            return _context.PartyMembers.Where(pm => pm.UserId == userId).Select(pm => pm.Party).FirstOrDefaultAsync();
        }

        public Task<Party?> GetByIdAsync(Guid partyId)
        {
            return _context.Parties.FirstOrDefaultAsync(p => p.Id == partyId);
        }

        public Task<Party?> GetByLeaderIdAsync(Guid leaderId)
        {
            return _context.Parties.FirstOrDefaultAsync(p => p.LeaderId == leaderId);
        }

        public Task<PartyMember?> GetMemberAsync(Guid partyId, Guid userId)
        {
            // note: PK of PartyMember = partyId + userId
            return _context.PartyMembers.FirstOrDefaultAsync(pm => pm.PartyId == partyId && pm.UserId == userId);
        }

        public Task<List<PartyMember>> GetMembersAsync(Guid partyId)
        {
            return _context.PartyMembers.Where(pm => pm.PartyId == partyId).ToListAsync();
        }

        public Task<int> GetMemberCountAsync(Guid partyId)
        {
            return _context.PartyMembers.CountAsync(pm => pm.PartyId == partyId);
        }

        public Task<PartyMember> GetOldestMemberAsync(Guid partyId)
        {
            return _context
                .PartyMembers.Where(pm => pm.PartyId == partyId && pm.Role == UserRole.Member)
                .OrderBy(pm => pm.JoinedAt)
                .FirstAsync();
        }

        public Task<bool> IsUserInPartyAsync(Guid partyId, Guid userId)
        {
            return _context.PartyMembers.AnyAsync(pm =>
                pm.UserId == userId && pm.PartyId == partyId
            );
        }

        public async Task RemoveMemberAsync(PartyMember member)
        {
           _context.PartyMembers.Remove(member);
           await _context.SaveChangesAsync();
        }

        public async Task UpdatePartyAsync(Party party)
        {
            _context.Parties.Update(party);
            await _context.SaveChangesAsync();
        }
    }
}
