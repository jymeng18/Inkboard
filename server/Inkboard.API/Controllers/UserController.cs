namespace Inkboard.API;

public static class UserController
{
    public static void MapUserController(this IEndpointRouteBuilder endpoint)
    {
        endpoint.MapGet("/health", () => Results.Ok()).RequireAuthorization();
    }
}
