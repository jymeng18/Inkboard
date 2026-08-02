namespace Inkboard.Application.Friends.DTO;

// RequesterId is the sender, echoed back from the entry in the caller's request list.
public record RespondFriendRequest(string RequesterId, bool Accepted);
