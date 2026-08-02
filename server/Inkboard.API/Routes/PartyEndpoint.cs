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
                .MapGet(
                    "/api/parties/{partyId}",
                    async (IPartyService partyService, Guid partyId, ClaimsPrincipal user) =>
                    {
                        var userId = user.GetUserId();
                        if (userId == Guid.Empty)
                            return Results.Unauthorized();

                        var result = await partyService.GetPartyByIdAsync(partyId);
                        if (!result.IsSuccess)
                            return ToErrorResult(result.Error, result.ErrorType);

                        return Results.Ok(result.Data);
                    }
                )
                .RequireAuthorization();

            endpoint
                .MapPost(
                    "/api/parties",
                    async (
                        IPartyService partyService,
                        ClaimsPrincipal user,
                        CreatePartyRequest request
                    ) =>
                    {
                        var leaderId = user.GetUserId();
                        if (leaderId == Guid.Empty)
                            return Results.Unauthorized();

                        var result = await partyService.CreatePartyAsync(
                            leaderId,
                            request.CanvasId
                        );
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

            // Leader ends the session: canvas closes for everyone, party dissolves.
            endpoint
                .MapPost(
                    "/api/parties/{partyId}/end",
                    async (IPartyService partyService, ClaimsPrincipal user, Guid partyId) =>
                    {
                        var leaderId = user.GetUserId();
                        if (leaderId == Guid.Empty)
                            return Results.Unauthorized();

                        var result = await partyService.EndSessionAsync(partyId, leaderId);
                        if (!result.IsSuccess)
                            return ToErrorResult(result.Error, result.ErrorType);

                        return Results.NoContent();
                    }
                )
                .RequireAuthorization();

            // Leader points the party at a canvas, pulling members in after them.
            endpoint
                .MapPatch(
                    "/api/parties/{partyId}/canvas",
                    async (
                        IPartyService partyService,
                        ClaimsPrincipal user,
                        Guid partyId,
                        SetPartyCanvasRequest request
                    ) =>
                    {
                        var leaderId = user.GetUserId();
                        if (leaderId == Guid.Empty)
                            return Results.Unauthorized();

                        if (
                            string.IsNullOrWhiteSpace(request.CanvasId)
                            || request.CanvasId.Length >= 50
                            || !Guid.TryParse(request.CanvasId, out var canvasId)
                        )
                        {
                            return Results.BadRequest("CanvasId is malformed or is too long.");
                        }

                        var result = await partyService.SetPartyCanvasAsync(
                            partyId,
                            leaderId,
                            canvasId
                        );
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

                        if (string.IsNullOrWhiteSpace(userRequest.InvitedUserId))
                        {
                            return Results.BadRequest("Invited userId cannot be empty.");
                        }

                        // Evaluate that InvitedUserId from reqbody is not some 100mb bullshit before trying to parse
                        if (
                            userRequest.InvitedUserId.Length >= 50
                            || !Guid.TryParse(userRequest.InvitedUserId, out var invitedUserId)
                        )
                        {
                            return Results.BadRequest("UserId is malformed or is too long.");
                        }

                        var result = await partyService.InviteUserAsync(
                            partyId,
                            leaderId,
                            invitedUserId
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

            endpoint.MapGet(
                "/api/invites",
                async (IPartyService partyService, ClaimsPrincipal user) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await partyService.GetPartyInvitesByUserIdAsync(userId);
                    // no such thing as this failing, since its either db exception(other issue) or just empty list
                    return Results.Ok(result.Data);
                }
            );
        }
    }
}
