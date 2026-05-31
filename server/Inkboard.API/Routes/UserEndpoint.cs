namespace Inkboard.API;

public static class UserEndpoint
{
    public static void MapUserEndpoint(this IEndpointRouteBuilder endpoint)
    {
        endpoint.MapGet("/health", () => Results.Ok()).RequireAuthorization();
    }
}
