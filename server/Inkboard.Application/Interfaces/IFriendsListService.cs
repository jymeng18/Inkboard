using Inkboard.Application.Common;
using Inkboard.Application.Friends.DTO;

namespace Inkboard.Application.Interfaces;

public interface IFriendsListService
{
    Task<Result<FriendRequestDto>> SendFriendReqAsync(Guid requesterId, Guid requesteeId);

    /* Returns the answered request. UserId/UserName on it is the sender, who is
     * now the new friend. */
    Task<Result<FriendRequestDto>> AcceptFriendReqAsync(
        Guid requestId,
        Guid requesterid,
        Guid requesteeId
    );

    Task<Result<FriendRequestDto>> RejectFriendReqAsync(
        Guid requestId,
        Guid requesterId,
        Guid requesteeId
    );

    /* Cancelling is the sender's move, the receiver rejects instead. */
    Task<Result> CancelFriendReqAsync(Guid requestId, Guid requesterId);

    Task<Result> UnfriendAsync(Guid userId, Guid targetUserId);

    // Get methods
    Task<Result<List<FriendDto>>> GetFriendsListByIdAsync(Guid userId);

    Task<Result<List<FriendRequestDto>>> GetPendingRequestsByIdAsync(Guid userId);

    Task<Result<List<FriendRequestDto>>> GetAllRequestsByIdAsync(Guid userId);
}
