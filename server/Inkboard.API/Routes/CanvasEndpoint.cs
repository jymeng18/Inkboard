using System.Security.Claims;
using Inkboard.Application.Canvases.DTO;
using Inkboard.Application.Common;
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

    private static IResult ToErrorResult(string? error, ErrorType errorType) =>
        errorType switch
        {
            ErrorType.NotFound => Results.NotFound(error),
            ErrorType.Forbidden => Results.Json(new { error }, statusCode: 403),
            ErrorType.Validation => Results.BadRequest(error),
            ErrorType.Conflict => Results.Conflict(error),
            _ => Results.Problem(error),
        };

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
                        return ToErrorResult(result.Error, result.ErrorType);

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
                        return ToErrorResult(result.Error, result.ErrorType); // * tried deleting a canvas not belong to you

                    return Results.NoContent();
                }
            )
            .RequireAuthorization();

        endpoint
            .MapPut(
                "/api/canvas/{canvasId}",
                async (
                    ClaimsPrincipal user,
                    Guid canvasId,
                    CanvasRequest canvasRequest,
                    ICanvasService canvasService
                ) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await canvasService.RenameCanvas(
                        canvasRequest.Name,
                        canvasId,
                        userId
                    );
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

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
