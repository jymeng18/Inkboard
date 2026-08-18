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

        endpoint
            .MapPost(
                "/api/canvas/{canvasId}/snapshot",
                async (
                    Guid canvasId,
                    IFormFile file,
                    ClaimsPrincipal user,
                    ICanvasService canvasService
                ) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    if (file is null || file.Length == 0)
                        return Results.BadRequest("Snapshot file is missing.");

                    await using var stream = file.OpenReadStream();
                    var result = await canvasService.UploadSnapshotAsync(
                        canvasId,
                        userId,
                        stream,
                        file.ContentType
                    );
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    return Results.NoContent();
                }
            )
            // ! in .net8, minimal apis only; endpoints that bind IFormFile enforces CSRF validation, we don't include it in frontend code, nor do we even need it
            .DisableAntiforgery()
            .RequireAuthorization();

        endpoint
            .MapGet(
                "/api/canvas/{canvasId}/snapshot",
                async (Guid canvasId, ClaimsPrincipal user, ICanvasService canvasService) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await canvasService.GetSnapshotAsync(canvasId, userId);
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    return Results.Stream(result.Data!, "image/png");
                }
            )
            .RequireAuthorization();

        endpoint
            .MapGet(
                "/api/canvas/{canvasId}/snapshot-preview",
                async (
                    Guid canvasId,
                    HttpContext context,
                    ClaimsPrincipal user,
                    ICanvasService canvasService
                ) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await canvasService.GetSnapshotPreviewAsync(canvasId, userId);
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    // * cache previews in user RAM, no Azure
                    context.Response.Headers.CacheControl = "private, max-age=31536000, immutable";
                    return Results.Stream(result.Data!, "image/png");
                }
            )
            .RequireAuthorization();

        endpoint
            .MapPost(
                "/api/canvas/{canvasId}/operations",
                async (
                    Guid canvasId,
                    OperationRequest request,
                    ClaimsPrincipal user,
                    ICanvasService canvasService
                ) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await canvasService.SaveOperationAsync(
                        canvasId,
                        userId,
                        request.Type,
                        request.OperationData
                    );
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    return Results.NoContent();
                }
            )
            .RequireAuthorization();

        endpoint
            .MapGet(
                "/api/canvas/{canvasId}/operations",
                async (Guid canvasId, ClaimsPrincipal user, ICanvasService canvasService) =>
                {
                    var userId = user.GetUserId();
                    if (userId == Guid.Empty)
                        return Results.Unauthorized();

                    var result = await canvasService.GetOperationsAsync(canvasId, userId);
                    if (!result.IsSuccess)
                        return ToErrorResult(result.Error, result.ErrorType);

                    return Results.Ok(result.Data);
                }
            )
            .RequireAuthorization();
    }
}
