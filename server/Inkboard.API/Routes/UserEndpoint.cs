using System.Security.Claims;
using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Users.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Inkboard.API.Routes;

public static class UserEndpoint
{
    private static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
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

    public static void MapUserEndpoint(this IEndpointRouteBuilder app)
    {
        var endpoint = app.MapGroup("").RequireRateLimiting("GeneralPolicy");
        endpoint.MapGet("/health", () => Results.Ok());

        endpoint.MapGet(
            "/api/users/me",
            async (ClaimsPrincipal user, IAuthService authService) =>
            {
                var userId = user.GetUserId();
                if (userId == Guid.Empty)
                    return Results.Unauthorized();

                var result = await authService.GetAccountAsync(userId);
                if (!result.IsSuccess)
                    return ToErrorResult(result.Error, result.ErrorType);

                return Results.Ok(result.Data);
            }
        ).RequireAuthorization();

        endpoint.MapDelete(
            "/api/users/me",
            async (
                ClaimsPrincipal user,
                IAuthService authService,
                HttpResponse response,
                [FromBody] DeleteAccountRequest request
            ) =>
            {
                var userId = user.GetUserId();
                if (userId == Guid.Empty)
                    return Results.Unauthorized();

                var result = await authService.DeleteAccountAsync(userId, request.Password);
                if (!result.IsSuccess)
                    return ToErrorResult(result.Error, result.ErrorType);

                response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth" });
                return Results.NoContent();
            }
        ).RequireAuthorization().RequireRateLimiting("AuthPolicy");
    }
}
