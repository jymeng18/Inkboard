using System.Security.Claims;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Parties.DTO;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;

namespace Inkboard.API.Routes
{
    public static class PartyEndpoint
    {
        public static void MapPartyEndpoint(this IEndpointRouteBuilder endpoint)
        {
            endpoint.MapPost(
                "/api/parties",
                async (IPartyService partyService, ClaimsPrincipal user) =>
                {
                    // pulling out a userId,
                    var leaderIdStr = user.FindFirst("sub")?.Value;
                    if (!Guid.TryParse(leaderIdStr, out var leaderId))
                    {
                        return Results.Unauthorized();
                    }

                    var party = await partyService.CreatePartyAsync(leaderId);

                    return Results.Created($"/api/parties/{party.Id}", party);
                }
            ).RequireAuthorization();

            // Leaving a party
            endpoint.MapDelete(
                "/api/parties/{partyId}",
                async (IPartyService partyService, ClaimsPrincipal user, Guid partyId) =>
                {
                    // pulling out a userId,
                    var userIdStr = user.FindFirst("sub")?.Value;
                    if (!Guid.TryParse(userIdStr, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    try
                    {
                        await partyService.LeavePartyAsync(partyId, userId);
                        return Results.NoContent();
                    }
                    catch (PartyNotFoundException ex)
                    {
                        return Results.NotFound(ex.Message);
                    }
                    catch (PartyForbiddenException ex)
                    {
                        return Results.Json(new { error = ex.Message }, statusCode: 403);
                    }
                    catch (PartyValidationException ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                }
            ).RequireAuthorization();

            // Invite a User (must be a leader)
            endpoint.MapPost(
                "/api/parties/{partyId}/invites",
                async (
                    IPartyService partyService,
                    Guid partyId,
                    ClaimsPrincipal user,
                    InviteUserRequest userRequest
                ) =>
                {
                    var leaderIdStr = user.FindFirst("sub")?.Value;
                    if (!Guid.TryParse(leaderIdStr, out var leaderId))
                    {
                        return Results.Unauthorized();
                    }

                    var invitedUserId = userRequest.InvitedUserId;

                    try
                    {
                        var invite = await partyService.InviteUserAsync(
                            partyId,
                            leaderId,
                            invitedUserId
                        );

                        return Results.Created(
                            $"/api/parties/{partyId}/invites/{invite.Id}",
                            invite
                        );
                    }
                    catch (PartyNotFoundException ex)
                    {
                        return Results.NotFound(ex.Message);
                    }
                    catch (PartyForbiddenException ex)
                    {
                        return Results.Json(new { error = ex.Message }, statusCode: 403);
                    }
                    catch (PartyValidationException ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                }
            ).RequireAuthorization();

            // Respond to a invite
            endpoint.MapPost(
                "/api/invites/{inviteId}/respond",
                async (
                    IPartyService partyService,
                    ClaimsPrincipal user,
                    Guid inviteId,
                    InviteRespondRequest respondRequest
                ) =>
                {
                    bool accpeted = respondRequest.accepted;

                    var userIdStr = user.FindFirst("sub")?.Value;
                    if (!Guid.TryParse(userIdStr, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    try
                    {
                        var inviteResponse = await partyService.RespondToUserInviteAsync(
                            inviteId,
                            userId,
                            accpeted
                        );
                        return Results.Ok(inviteResponse);
                    }
                    catch (PartyNotFoundException ex)
                    {
                        return Results.NotFound(ex.Message);
                    }
                    catch (PartyForbiddenException ex)
                    {
                        return Results.Json(new { error = ex.Message }, statusCode: 403);
                    }
                    catch (PartyValidationException ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                }
            ).RequireAuthorization();

            // Kicking out a member ( only allwoed to be done by leader)
            endpoint.MapDelete(
                "/api/parties/{partyId}/members/{targetUserId}",
                async (
                    Guid partyId,
                    Guid targetUserId,
                    IPartyService partyService,
                    ClaimsPrincipal user
                ) =>
                {
                    var leaderIdStr = user.FindFirst("sub")?.Value;
                    if (!Guid.TryParse(leaderIdStr, out var leaderId))
                    {
                        return Results.Unauthorized();
                    }

                    try
                    {
                        await partyService.RemoveMemberAsync(partyId, leaderId, targetUserId);
                        return Results.NoContent();
                    }
                    catch (PartyNotFoundException ex)
                    {
                        return Results.NotFound(ex.Message);
                    }
                    catch (PartyForbiddenException ex)
                    {
                        return Results.Json(new { error = ex.Message }, statusCode: 403);
                    }
                    catch (PartyValidationException ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                }
            ).RequireAuthorization();

            endpoint.MapPost(
                "/api/users/{targetUserId}/block",
                async (Guid targetUserId, IPartyService partyService, ClaimsPrincipal user) =>
                {
                    var userIdStr = user.FindFirst("sub")?.Value;
                    if (!Guid.TryParse(userIdStr, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    try
                    {
                        await partyService.BlockUserAsync(userId, targetUserId);
                        return Results.NoContent();
                    }
                    catch (PartyValidationException ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                }
            ).RequireAuthorization();
        }
    }
}
