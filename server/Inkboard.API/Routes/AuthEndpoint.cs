using FluentValidation;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.API.Routes;

public static class AuthEndpoint
{
    public static void MapAuthEndpoint(this IEndpointRouteBuilder endpoint)
    {
        endpoint.MapPost(
            "/login",
            async (
                LoginRequest request,
                ITokenGenerator tokenGenerator,
                IUserRepository repository
            ) =>
            {
                var user = await repository.FindByEmailAsync(request.Email);
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                // compare hashpw <-> pw
                bool validPassword = BCrypt.Net.BCrypt.EnhancedVerify(
                    request.Password,
                    user.PasswordHash
                );
                if (!validPassword)
                {
                    return Results.Problem(detail: "Invlaid password or email.", statusCode: 401);
                }

                var token = tokenGenerator.GenerateToken(user.Id, request.Email);

                return Results.Ok(new { access_token = token });
            }
        );

        endpoint.MapPost(
            "/register",
            async (
                RegisterRequest request,
                IUserRepository repository,
                IValidator<RegisterRequest> validator
            ) =>
            {
                bool emailExists = await repository.EmailExistsAsync(request.Email);
                if (emailExists)
                {
                    return Results.Conflict(new { message = "Email is already registered." }); // 409
                }

                // Authorize all fields in request
                var result = await validator.ValidateAsync(request);
                if (!result.IsValid)
                {
                    var errors = result
                        .Errors.GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    return Results.ValidationProblem(errors);
                }

                // Create new user to save to db(Users)
                User user = new User
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password, 12), // workfactor = 12
                };

                try
                {
                    await repository.CreateUserAsync(user);
                    return Results.Created($"/users/{user.Id}", new { user.Id });
                }
                catch(DbUpdateException)
                {
                    return Results.Problem("Could not save user.");                    
                }
                catch (Exception)
                {
                    return Results.Problem("An unexpected error occured.");
                }
            }
        );
    }
}
