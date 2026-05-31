using Inkboard.Application;
using Inkboard.Domain;
using Inkboard.Infra;
using Microsoft.AspNetCore.Identity.Data;

namespace Inkboard.API;

public static class AuthEndpoint
{
    public static void MapAuthEndpoint(this IEndpointRouteBuilder endpoint)
    {
        endpoint.MapPost(
            "/login",
            async (LoginRequest request, TokenGenerator tokenGenerator, IUserRepository repository) =>
            {
                var user = await repository.FindByEmailAsync(request.Email);
                if(user is null)
                {
                    return Results.Unauthorized();
                }

                // compare hashpw <-> pw
                bool validPassword = BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash);
                if (!validPassword)
                {
                    return Results.Unauthorized();
                }

                var token = tokenGenerator.GenerateToken(user.Id, request.Email);


                return Results.Ok(new { access_token = token });
            }
        );





    }
}
