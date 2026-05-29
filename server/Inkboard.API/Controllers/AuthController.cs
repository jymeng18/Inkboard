using Inkboard.Application;
using Microsoft.AspNetCore.Identity.Data;

namespace Inkboard.API;

public static class AuthController
{
    public static void MapAuthController(this IEndpointRouteBuilder endpoint)
    {
        endpoint.MapPost(
            "/login",
            (LoginRequest request, TokenGenerator tokenGenerator) =>
            {
                var mockUserId = Guid.NewGuid();
                var token = tokenGenerator.GenerateToken(mockUserId, request.Email);

                return Results.Ok(new { access_token = token });
            }
        );
    }
}
