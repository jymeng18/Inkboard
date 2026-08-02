using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db
{
    public class PartyInviteRepository : IPartyInviteRepository
    {
        private readonly AppDbContext _context;

        public PartyInviteRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<PartyInvite?> GetByIdAsync(Guid inviteId)
        {
            return _context.PartyInvites.FirstOrDefaultAsync(pi => pi.Id == inviteId);
        }

        public Task<PartyInvite?> GetPendingInviteAsync(Guid partyId, Guid invitedUserId)
        {
            return _context.PartyInvites.FirstOrDefaultAsync(pi =>
                pi.PartyId == partyId
                && pi.InvitedUserId == invitedUserId
                && pi.InviteStatus == InviteStatus.Pending
            );
        }

        public async Task CreateInviteAsync(PartyInvite invite)
        {
            _context.PartyInvites.Add(invite);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateInviteAsync(PartyInvite invite)
        {
            _context.PartyInvites.Update(invite);
            await _context.SaveChangesAsync();
        }

        public Task<List<PartyInvite>> GetAllPendingByUserIdAsync(Guid userId)
        {
            return _context
                .PartyInvites.Where(pi =>
                    pi.InvitedUserId == userId && pi.InviteStatus == InviteStatus.Pending
                )
                .ToListAsync();
        }
    }
}
