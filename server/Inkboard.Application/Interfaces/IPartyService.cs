using Inkboard.Application.Common;
using Inkboard.Application.Parties.DTO;
using Inkboard.Domain.Models;

namespace Inkboard.Application.Interfaces
{
    public interface IPartyService
    {
        Task<Result<Party>> CreatePartyAsync(Guid leaderId, Guid canvasId);
        Task<Result<PartyDetailDto>> GetPartyByIdAsync(Guid partyId);
        Task<Result<List<PartyInvite>>> GetPartyInvitesByUserIdAsync(Guid userId);
        Task<Result<PartyInvite>> InviteUserAsync(Guid partyId, Guid leaderId, Guid invitedUserId);
        Task<Result<PartyInvite>> RespondToUserInviteAsync(Guid id, Guid userId, bool accepted);
        Task<Result> RemoveMemberAsync(Guid partyId, Guid leaderId, Guid targetUserId);
        Task<Result> LeavePartyAsync(Guid partyId, Guid userId);
        Task<Result> BlockUserAsync(Guid leaderId, Guid targetUserId);

        /* Leader-only teardown: closes the canvas for everyone and dissolves the
         * party outright, as opposed to LeavePartyAsync which hands leadership on. */
        Task<Result> EndSessionAsync(Guid partyId, Guid leaderId);

        /* Points an existing party at a canvas and pulls its members in after the
         * leader. CreatePartyAsync only covers the first canvas a party ever has. */
        Task<Result> SetPartyCanvasAsync(Guid partyId, Guid leaderId, Guid canvasId);
    }
}
