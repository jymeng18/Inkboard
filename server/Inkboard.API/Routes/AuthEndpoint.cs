using FluentValidation;
using Inkboard.Application.Auth.DTO;
using Inkboard.Application.Interfaces;

namespace Inkboard.API.Routes;

public static class AuthEndpoint
{
    public static void MapAuthEndpoint(this IEndpointRouteBuilder endpoint)
    {
        endpoint.MapPost(
            "/api/auth/login",
            async (LoginRequestModel request, IAuthService authService) =>
            {
                var result = await authService.LoginAsync(request);
                if (!result.Success)
                {
                    return Results.Problem(detail: result.ErrorMessage, statusCode: 401); // 401 unauthorized
                }
                return Results.Ok(new { access_token = result.AccessToken });
            }
        );

        endpoint.MapPost(
            "/api/auth/register",
            async (RegisterRequestModel request, IAuthService authService) =>
            {
                var result = await authService.RegisterAsync(request);
                if (!result.Success)
                {
                    if (result.ValidationErrors is not null)
                    {
                        return Results.ValidationProblem(result.ValidationErrors);
                    }
                    if (result.ErrorMessage == "Email is already registered.")
                    {
                        return Results.Conflict(new { message = result.ErrorMessage });
                    }
                }
                return Results.Created($"/users/{result.UserId}", new { Id = result.UserId });
            }
        );

        endpoint.MapPost(
            "/api/auth/logout",
            async (RefreshRequestModel request, IAuthService authService) =>
            {
                var result = await authService.LogoutAsync(request.RefreshToken);
                return Results.Ok(result);
            }
        );

        endpoint.MapPost(
            "/api/auth/refresh",
            async (RefreshRequestModel request, IAuthService authService) =>
            {
                var result = await authService.RefreshAsync(request.RefreshToken);
                if (!result.Success)
                {
                    return Results.Problem(detail: result.ErrorMessage, statusCode: 401);
                }
                return Results.Ok(
                    new { access_token = result.AccessToken, refresh_token = result.RefreshToken }
                );
            }
        );
    }
}
