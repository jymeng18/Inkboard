using Inkboard.Application.Common;
using Inkboard.Application.Parties.DTO;
using Inkboard.Domain.Models;

namespace Inkboard.Application.Interfaces
{
    public interface IPartyService
    {
        Task<Result<Party>> CreatePartyAsync(Guid leaderId, Guid canvasId);
        Task<Result<PartyDetailDto>> GetPartyByIdAsync(Guid partyId);
        Task<Result<PartyInvite>> InviteUserAsync(Guid partyId, Guid leaderId, Guid invitedUserId);
        Task<Result<PartyInvite>> RespondToUserInviteAsync(Guid id, Guid userId, bool accepted);
        Task<Result> RemoveMemberAsync(Guid partyId, Guid leaderId, Guid targetUserId);
        Task<Result> LeavePartyAsync(Guid partyId, Guid userId);
        Task<Result> BlockUserAsync(Guid leaderId, Guid targetUserId);
    }
}
