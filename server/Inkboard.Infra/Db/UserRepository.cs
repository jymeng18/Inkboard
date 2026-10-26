using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> FindByEmailAsync(string email)
    {
        return _context.Users.FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Email == email);
        return user != null;
    }

    public async Task CreateUserAsync(User user)
    {
        // Note: this may look like it needs to be asynchronous, but it operatoes directly on RAM instead  of DB
        _context.Users.Add(user);

        // Commit changes to DB
        await _context.SaveChangesAsync();
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _context.Users.FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task DeleteUserAsync(User user)
    {
        /*
        Honeslty not ideal, we load all related entities into memory
        and then delete the user, which cascade deletes related rows
        Note: we need to do this when using in-memory provider, since
        it does not know about fk restirctions
        */

        var canvasIds = await _context
            .Canvas.Where(canvas => canvas.OwnerId == user.Id)
            .Select(canvas => canvas.Id)
            .ToListAsync();
        await _context.RefreshTokens.Where(token => token.UserId == user.Id).LoadAsync();
        await _context.PartyMembers.Where(member => member.UserId == user.Id).LoadAsync();
        await _context
            .PartyInvites.Where(invite =>
                invite.InvitedByUserId == user.Id || invite.InvitedUserId == user.Id
            )
            .LoadAsync();
        await _context
            .BlockLists.Where(block => block.UserId == user.Id || block.BlockedUserId == user.Id)
            .LoadAsync();
        await _context
            .FriendRequests.Where(request =>
                request.RequesterId == user.Id || request.RequesteeId == user.Id
            )
            .LoadAsync();
        await _context
            .Friendships.Where(friendship =>
                friendship.UserId1 == user.Id || friendship.UserId2 == user.Id
            )
            .LoadAsync();
        await _context
            .Parties.Where(party =>
                party.CanvasId.HasValue && canvasIds.Contains(party.CanvasId.Value)
            )
            .LoadAsync();
        await _context.Canvas.Where(canvas => canvas.OwnerId == user.Id).LoadAsync();
        await _context
            .CanvasOperations.Where(operation =>
                operation.UserId == user.Id || canvasIds.Contains(operation.CanvasId)
            )
            .LoadAsync();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}
