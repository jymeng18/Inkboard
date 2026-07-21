using System.Security.Claims;
using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Parties.DTO;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Inkboard.API.Routes
{
    public static class PartyEndpoint
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

        public static void MapPartyEndpoint(this IEndpointRouteBuilder endpoint)
        {
            endpoint
                .MapPost(
                    "/api/parties",
                    async (IPartyService partyService, ClaimsPrincipal user, CreatePartyRequest request) =>
                    {
                        var leaderId = user.GetUserId();
                        if (leaderId == Guid.Empty)
                            return Results.Unauthorized();

                        var result = await partyService.CreatePartyAsync(leaderId, request.CanvasId);
                        if (!result.IsSuccess)
                            return ToErrorResult(result.Error, result.ErrorType);

                        return Results.Created($"/api/parties/{result.Data!.Id}", result.Data);
                    }
                )
                .RequireAuthorization();

            endpoint
                .MapDelete(
                    "/api/parties/{partyId}",
                    async (IPartyService partyService, ClaimsPrincipal user, Guid partyId) =>
                    {
                        var userId = user.GetUserId();
                        if (userId == Guid.Empty)
                            return Results.Unauthorized();

                        var result = await partyService.LeavePartyAsync(partyId, userId);
                        if (!result.IsSuccess)
                            return ToErrorResult(result.Error, result.ErrorType);

                        return Results.NoContent();
                    }
                )
                .RequireAuthorization();

            endpoint
                .MapPost(
                    "/api/parties/{partyId}/invites",
                    async (
                        IPartyService partyService,
                        Guid partyId,
                        ClaimsPrincipal user,
                        InviteUserRequest userRequest
                    ) =>
                    {
                        var leaderId = user.GetUserId();
                        if (leaderId == Guid.Empty)
                            return Results.Unauthorized();

                        var result = await partyService.InviteUserAsync(
                            partyId,
                            leaderId,
                            userRequest.InvitedUserId
                        );
                        if (!result.IsSuccess)
                            return ToErrorResult(result.Error, result.ErrorType);

                        return Results.Created(
                            $"/api/parties/{partyId}/invites/{result.Data!.Id}",
                            result.Data
                        );
                    }
                )
                .RequireAuthorization();

            endpoint
                .MapPost(
                    "/api/invites/{inviteId}/respond",
                    async (
                        IPartyService partyService,
                        ClaimsPrincipal user,
                        Guid inviteId,
                        InviteRespondRequest respondRequest
                    ) =>
                    {
                        var userId = user.GetUserId();
                        if (userId == Guid.Empty)
                            return Results.Unauthorized();

                        var result = await partyService.RespondToUserInviteAsync(
                            inviteId,
                            userId,
                            respondRequest.accepted
                        );
                        if (!result.IsSuccess)
                            return ToErrorResult(result.Error, result.ErrorType);

                        return Results.Ok(result.Data);
                    }
                )
                .RequireAuthorization();

            endpoint
                .MapDelete(
                    "/api/parties/{partyId}/members/{targetUserId}",
                    async (
                        Guid partyId,
                        Guid targetUserId,
                        IPartyService partyService,
                        ClaimsPrincipal user
                    ) =>
                    {
                        var leaderId = user.GetUserId();
                        if (leaderId == Guid.Empty)
                            return Results.Unauthorized();

                        var result = await partyService.RemoveMemberAsync(
                            partyId,
                            leaderId,
                            targetUserId
                        );
                        if (!result.IsSuccess)
                            return ToErrorResult(result.Error, result.ErrorType);

                        return Results.NoContent();
                    }
                )
                .RequireAuthorization();

            endpoint
                .MapPost(
                    "/api/users/{targetUserId}/block",
                    async (Guid targetUserId, IPartyService partyService, ClaimsPrincipal user) =>
                    {
                        var userId = user.GetUserId();
                        if (userId == Guid.Empty)
                            return Results.Unauthorized();

                        var result = await partyService.BlockUserAsync(userId, targetUserId);
                        if (!result.IsSuccess)
                            return ToErrorResult(result.Error, result.ErrorType);

                        return Results.NoContent();
                    }
                )
                .RequireAuthorization();
        }
    }
}
