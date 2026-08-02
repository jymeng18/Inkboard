using Inkboard.Application.Common;
using Inkboard.Application.Friends.DTO;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;

namespace Inkboard.Application.Services;

public class FriendsListService : IFriendsListService
{
    private readonly IFriendRequestRepository _frReqRepository;
    private readonly IFriendshipRepository _frshipRepository;
    private readonly IUserRepository _userRepository;

    public FriendsListService(
        IFriendRequestRepository frReqRepository,
        IFriendshipRepository frshipRepository,
        IUserRepository userRepository
    )
    {
        _frReqRepository = frReqRepository;
        _frshipRepository = frshipRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<FriendRequestDto>> SendFriendReqAsync(Guid senderId, Guid receiverId)
    {
        // Verify receiver exist
        var receiver = await _userRepository.GetByIdAsync(receiverId);
        if (receiver is null)
        {
            return Result<FriendRequestDto>.Fail(ErrorType.NotFound, "User does not exist.");
        }

        bool isFriends = await _frshipRepository.AreFriendsAsync(senderId, receiverId);
        if (isFriends)
        {
            return Result<FriendRequestDto>.Fail(ErrorType.Validation, "You're already friends.");
        }

        if (receiverId == senderId)
        {
            return Result<FriendRequestDto>.Fail(
                ErrorType.Validation,
                "You cannot send a friend request to yourself."
            );
        }

        // Check if you have alr sent them a request
        var sentRequest = await _frReqRepository.GetPendingBetweenAsync(senderId, receiverId);
        if (sentRequest is not null)
        {
            return Result<FriendRequestDto>.Fail(
                ErrorType.Conflict,
                "You have already sent this user a friend request."
            );
        }

        // Check if your "receiver" has sent you a request
        var receiverSentRequest = await _frReqRepository.GetPendingBetweenAsync(
            receiverId,
            senderId
        );
        if (receiverSentRequest is not null)
        {
            return Result<FriendRequestDto>.Fail(
                ErrorType.Conflict,
                "Check your inbox. This user has sent you a friend request already."
            );
        }

        FriendRequest friendRequest = new()
        {
            RequesterId = senderId,
            RequesteeId = receiverId,
            Status = RequestStatus.Pending,
        };
        await _frReqRepository.CreateFriendReqAsync(friendRequest);

        /// <summary>
        /// This looks wrong because we think if a sender sends a req, shouldn't receiver know WHO sent the request?
        /// --> This is data being ret to the CALLEr of the API, so they see: "You sent a request to {some_uid}."
        /// </summary>
        return Result<FriendRequestDto>.Ok(
            new FriendRequestDto(
                friendRequest.Id,
                receiver.Id,
                receiver.UserName,
                friendRequest.CreatedAt,
                RequestStatus.Pending
            )
        );
    }

    public async Task<Result<FriendRequestDto>> AcceptFriendReqAsync(
        Guid requestId,
        Guid senderId,
        Guid receiverId
    )
    {
        var friendRequest = await _frReqRepository.GetByIdAsync(requestId);
        if (friendRequest is null)
        {
            return Result<FriendRequestDto>.Fail(ErrorType.NotFound, "Friend request not found.");
        }

        // Check users exist
        var reciever = await _userRepository.GetByIdAsync(receiverId);
        if (reciever is null)
        {
            return Result<FriendRequestDto>.Fail(ErrorType.NotFound, "User(receiver) does not exist.");
        }

        var sender = await _userRepository.GetByIdAsync(senderId);
        if (sender is null)
        {
            return Result<FriendRequestDto>.Fail(ErrorType.NotFound, "User(sender) does not exist.");
        }

        if (friendRequest.RequesteeId != reciever.Id || friendRequest.RequesterId != sender.Id)
        {
            return Result<FriendRequestDto>.Fail(ErrorType.Forbidden, "Invalid receiver or sender!");
        }

        // Check to make sure friendship doesn't exist alr
        bool friendshipExists = await _frshipRepository.AreFriendsAsync(senderId, receiverId);
        if (friendshipExists)
        {
            return Result<FriendRequestDto>.Fail(ErrorType.Validation, "You are already friends.");
        }

        // Check to make sure request was not revoked
        if (friendRequest.Status != RequestStatus.Pending)
        {
            return Result<FriendRequestDto>.Fail(
                ErrorType.NotFound,
                "Friend request revoked/accepted/declined."
            );
        }

        // update status
        friendRequest.Status = RequestStatus.Accepted;
        await _frReqRepository.UpdateFriendReqAsync(friendRequest);

        // dont worry, repo method orders id for us
        Friendship newFriendship = new() { UserId1 = senderId, UserId2 = receiverId };

        await _frshipRepository.CreateFriendshipAsync(newFriendship);

        // Answering a request returns the request, so accept and reject share one
        // response shape. UserId/UserName is the sender, who is now the new friend,
        // so the caller can add them to its list straight off this.
        return Result<FriendRequestDto>.Ok(
            new FriendRequestDto(
                friendRequest.Id,
                sender.Id,
                sender.UserName,
                friendRequest.CreatedAt,
                RequestStatus.Accepted
            )
        );
    }

    public async Task<Result<FriendRequestDto>> RejectFriendReqAsync(
        Guid requestId,
        Guid requesterId,
        Guid requesteeId
    )
    {
        var friendRequest = await _frReqRepository.GetByIdAsync(requestId);
        if (friendRequest is null)
        {
            return Result<FriendRequestDto>.Fail(ErrorType.NotFound, "Friend request not found.");
        }

        var receiver = await _userRepository.GetByIdAsync(requesteeId);
        if (receiver is null)
        {
            return Result<FriendRequestDto>.Fail(
                ErrorType.NotFound,
                "User(receiver) does not exist."
            );
        }

        var sender = await _userRepository.GetByIdAsync(requesterId);
        if (sender is null)
        {
            return Result<FriendRequestDto>.Fail(
                ErrorType.NotFound,
                "User(sender) does not exist."
            );
        }

        if (friendRequest.RequesteeId != receiver.Id || friendRequest.RequesterId != sender.Id)
        {
            return Result<FriendRequestDto>.Fail(
                ErrorType.Forbidden,
                "Invalid sender or receiver!"
            );
        }

        // Make sure req was not revoked
        if (friendRequest.Status != RequestStatus.Pending)
        {
            return Result<FriendRequestDto>.Fail(
                ErrorType.NotFound,
                "Friend request was revoked/accepted/declined."
            );
        }
        friendRequest.Status = RequestStatus.Declined;
        await _frReqRepository.UpdateFriendReqAsync(friendRequest);

        return Result<FriendRequestDto>.Ok(
            new FriendRequestDto(
                friendRequest.Id,
                sender.Id,
                sender.UserName,
                friendRequest.CreatedAt,
                RequestStatus.Declined
            )
        );
    }

    public async Task<Result> CancelFriendReqAsync(Guid requestId, Guid requesterId)
    {
        var friendRequest = await _frReqRepository.GetByIdAsync(requestId);
        if (friendRequest is null)
        {
            return Result.Fail(ErrorType.NotFound, "Friend request not found.");
        }

        // Only the person who sent it can take it back.
        if (friendRequest.RequesterId != requesterId)
        {
            return Result.Fail(ErrorType.Forbidden, "You are not the sender!");
        }

        // Anything already answered stays answered. Revoking an accepted request
        // would leave the friendship it created with no record behind it.
        if (friendRequest.Status != RequestStatus.Pending)
        {
            return Result.Fail(ErrorType.Conflict, "This request has already been responded to.");
        }

        await _frReqRepository.RevokeFriendReqAsync(friendRequest);
        return Result.Ok();
    }

    public async Task<Result> UnfriendAsync(Guid userId, Guid targetUserId)
    {
        var targetUser = await _userRepository.GetByIdAsync(targetUserId);
        if (targetUser is null)
        {
            return Result.Fail(ErrorType.NotFound, "User does not exist.");
        }

        var friendship = await _frshipRepository.GetByIdAsync(userId, targetUserId);
        if (friendship is null)
        {
            return Result.Fail(ErrorType.NotFound, "Friendship not found.");
        }

        await _frshipRepository.DeleteFriendshipAsync(friendship);
        return Result.Ok();
    }

    // Get methods
    public async Task<Result<List<FriendDto>>> GetFriendsListByIdAsync(Guid userId)
    {
        List<User> friends = await _frshipRepository.GetFriendsListByIdAsync(userId);
        return Result<List<FriendDto>>.Ok(
            data: friends.ConvertAll(f => new FriendDto(f.Id, f.UserName))
        );
    }

    public async Task<Result<List<FriendRequestDto>>> GetPendingRequestsByIdAsync(Guid receiverId)
    {
        List<FriendRequest> friendRequests = await _frReqRepository.GetPendingReqsByUserIdAsync(
            receiverId
        );
        return Result<List<FriendRequestDto>>.Ok(
            friendRequests.ConvertAll(fr => new FriendRequestDto(
                fr.Id,
                fr.RequesterId,
                fr.Requester.UserName,
                fr.CreatedAt
            ))
        );
    }

    public async Task<Result<List<FriendRequestDto>>> GetAllRequestsByIdAsync(Guid userId)
    {
        List<FriendRequest> allFriendRequests = await _frReqRepository.GetAllReqsByUserIdAsync(
            userId
        );
        return Result<List<FriendRequestDto>>.Ok(
            allFriendRequests.ConvertAll(afr => new FriendRequestDto(
                afr.Id,
                afr.RequesterId,
                afr.Requester.UserName,
                afr.CreatedAt,
                afr.Status
            ))
        );
    }
}
