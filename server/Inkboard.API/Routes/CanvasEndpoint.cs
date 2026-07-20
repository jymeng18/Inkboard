using System.Security.Claims;
using Inkboard.Application.Canvases.DTO;
using Inkboard.Application.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Inkboard.API.Routes;

public static class CanvasEndpoint
{
    private static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdStr =
            user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
    }

    public static void MapCanvasEndpoint(this IEndpointRouteBuilder endpoint)
    {
        endpoint
            .MapPost(
                "/api/canvas",
                async (
                    ClaimsPrincipal user,
                    ICanvasService canvasService,
                    CanvasRequest canvasRequest
                ) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await canvasService.CreateCanvasAsync(userId, canvasRequest.Name);
                    if (!result.IsSuccess)
                        return Results.BadRequest(new { error = result.Error });

                    return Results.Created($"/api/canvas/{result.Data!.Id}", result.Data);
                }
            )
            .RequireAuthorization();

        endpoint
            .MapDelete(
                "/api/canvas/{canvasId}",
                async (Guid canvasId, ClaimsPrincipal user, ICanvasService canvasService) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await canvasService.DeleteCanvasAsync(canvasId, userId);
                    if (!result.IsSuccess)
                    {
                        return result.Error == "Canvas not found."
                            ? Results.NotFound(new { error = result.Error })
                            : Results.Json(new { error = result.Error }, statusCode: 403); // * tried deleting a canvas not belong to you
                    }

                    return Results.NoContent();
                }
            )
            .RequireAuthorization();

        endpoint
            .MapPut(
                "/api/canvas/{canvasId}",
                async (Guid canvasId, CanvasRequest canvasRequest, ICanvasService canvasService) =>
                {
                    var result = await canvasService.RenameCanvas(canvasRequest.Name, canvasId);
                    if (!result.IsSuccess)
                        return Results.NotFound(new { error = result.Error });

                    return Results.NoContent();
                }
            )
            .RequireAuthorization();

        endpoint
            .MapGet(
                "/api/canvas",
                async (ClaimsPrincipal user, ICanvasService canvasService) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await canvasService.GetAllCanvasesAsync(userId);
                    return Results.Ok(result.Data);
                }
            )
            .RequireAuthorization();
    }
}
