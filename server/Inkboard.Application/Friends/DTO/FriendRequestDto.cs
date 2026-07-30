using Inkboard.Domain.Models;

namespace Inkboard.Application.Friends.DTO;

// UserId/UserName is the other person in the request, relative to the caller:
// the sender when listing or answering requests, the receiver when sending one.
public record FriendRequestDto(
    Guid Id,
    Guid UserId,
    string UserName,
    DateTime CreatedAt,
    RequestStatus? Status = RequestStatus.Pending
);
