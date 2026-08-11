using Inkboard.Application.Auth.DTO;
using Inkboard.Application.Interfaces;

namespace Inkboard.API.Routes;

public static class AuthEndpoint
{
    private const string RefreshCookie = "refreshToken";

    private static CookieOptions RefreshCookieOptions(IWebHostEnvironment env) =>
        new()
        {
            HttpOnly = true,
            Secure = !(env.IsDevelopment() || env.IsEnvironment("Testing")),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        };

    public static void MapAuthEndpoint(this IEndpointRouteBuilder endpoint)
    {
        endpoint.MapPost(
            "/api/auth/login",
            async (
                LoginRequestModel request,
                IAuthService authService,
                HttpResponse response,
                IWebHostEnvironment env
            ) =>
            {
                var result = await authService.LoginAsync(request);
                if (result.ValidationErrors is not null)
                {
                    return Results.ValidationProblem(result.ValidationErrors);
                }

                if (!result.Success)
                {
                    return Results.Problem(detail: result.ErrorMessage, statusCode: 401);
                }

                response.Cookies.Append(RefreshCookie, result.RefreshToken!, RefreshCookieOptions(env));
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

        endpoint
            .MapPost(
                "/api/auth/logout",
                async (HttpRequest httpRequest, HttpResponse response, IAuthService authService) =>
                {
                    var refreshToken = httpRequest.Cookies[RefreshCookie];
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await authService.LogoutAsync(refreshToken);
                    }

                    response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/auth" });
                    return Results.Ok(new { success = true });
                }
            )
            .RequireAuthorization();

        endpoint.MapPost(
            "/api/auth/refresh",
            async (
                HttpRequest httpRequest,
                HttpResponse response,
                IAuthService authService,
                IWebHostEnvironment env
            ) =>
            {
                var refreshToken = httpRequest.Cookies[RefreshCookie];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Results.Problem(detail: "Missing refresh token.", statusCode: 401);
                }

                var result = await authService.RefreshAsync(refreshToken);
                if (!result.Success)
                {
                    return Results.Problem(detail: result.ErrorMessage, statusCode: 401);
                }

                response.Cookies.Append(RefreshCookie, result.RefreshToken!, RefreshCookieOptions(env));
                return Results.Ok(new { access_token = result.AccessToken });
            }
        );
    }
}
