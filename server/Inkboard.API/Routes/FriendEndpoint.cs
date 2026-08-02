using System.Security.Claims;
using Inkboard.Application.Common;
using Inkboard.Application.Friends.DTO;
using Inkboard.Application.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Inkboard.API.Routes;

public static class FriendEndpoint
{
    private static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdStr =
            user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
    }

    private static IResult ToErrorResult(string? error, ErrorType errorType) =>
        errorType switch
        {
            ErrorType.NotFound => Results.NotFound(error),
            ErrorType.Forbidden => Results.Json(new { error }, statusCode: 403),
            ErrorType.Validation => Results.BadRequest(error),
            ErrorType.Conflict => Results.Conflict(error),
            _ => Results.Problem(error),
        };

    public static void MapFriendEndpoint(this IEndpointRouteBuilder endpoint)
    {
        // Sending a friend request to another user
        endpoint
            .MapPost(
                "/api/friends/{targetUserId}",
                async (
                    IFriendsListService friendsService,
                    ClaimsPrincipal user,
                    Guid targetUserId
                ) =>
                {
                    var senderId = user.GetUserId();
                    if (senderId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await friendsService.SendFriendReqAsync(senderId, targetUserId);
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    return Results.Created($"/api/friend-requests/{result.Data!.Id}", result.Data);
                }
            )
            .RequireAuthorization();

        // Get friends list
        endpoint
            .MapGet(
                "/api/friends",
                async (IFriendsListService friendsService, ClaimsPrincipal user) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await friendsService.GetFriendsListByIdAsync(userId);
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    return Results.Ok(result.Data);
                }
            )
            .RequireAuthorization();

        // Unfriend/delete a user off friends list
        endpoint
            .MapDelete(
                "/api/friends/{targetUserId}",
                async (
                    IFriendsListService friendsService,
                    ClaimsPrincipal user,
                    Guid targetUserId
                ) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await friendsService.UnfriendAsync(userId, targetUserId);
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    return Results.NoContent();
                }
            )
            .RequireAuthorization();

        // Get friend requests sent to you
        endpoint
            .MapGet(
                "/api/friend-requests",
                async (IFriendsListService friendsService, ClaimsPrincipal user) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await friendsService.GetAllRequestsByIdAsync(userId);
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    return Results.Ok(result.Data);
                }
            )
            .RequireAuthorization();

        // accepting/rejecting reqs
        endpoint
            .MapPatch(
                "/api/friend-requests/{requestId}",
                async (
                    IFriendsListService friendsService,
                    ClaimsPrincipal user,
                    Guid requestId,
                    RespondFriendRequest respondRequest
                ) =>
                {
                    var receiverId = user.GetUserId();
                    if (receiverId == Guid.Empty)
                        return Results.Unauthorized();

                    // Length checked before parsing so a 100mb body never reaches TryParse
                    if (
                        string.IsNullOrWhiteSpace(respondRequest.RequesterId)
                        || respondRequest.RequesterId.Length >= 50
                        || !Guid.TryParse(respondRequest.RequesterId, out var senderId)
                    )
                    {
                        return Results.BadRequest("RequesterId is malformed or is too long.");
                    }

                    if (respondRequest.Accepted)
                    {
                        var accepted = await friendsService.AcceptFriendReqAsync(
                            requestId,
                            senderId,
                            receiverId
                        );
                        if (!accepted.IsSuccess)
                            return ToErrorResult(accepted.Error, accepted.ErrorType);

                        return Results.Ok(accepted.Data);
                    }

                    var rejected = await friendsService.RejectFriendReqAsync(
                        requestId,
                        senderId,
                        receiverId
                    );
                    if (!rejected.IsSuccess)
                        return ToErrorResult(rejected.Error, rejected.ErrorType);

                    return Results.Ok(rejected.Data);
                }
            )
            .RequireAuthorization();

        // Revoking a sent out friend request
        endpoint
            .MapDelete(
                "/api/friend-requests/{requestId}",
                async (IFriendsListService friendsService, ClaimsPrincipal user, Guid requestId) =>
                {
                    var requesterId = user.GetUserId();
                    if (requesterId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await friendsService.CancelFriendReqAsync(requestId, requesterId);
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    return Results.NoContent();
                }
            )
            .RequireAuthorization();
    }
}
