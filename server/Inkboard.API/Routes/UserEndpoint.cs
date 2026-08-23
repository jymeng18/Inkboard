namespace Inkboard.API.Routes;

public static class UserEndpoint
{
    public static void MapUserEndpoint(this IEndpointRouteBuilder app)
    {
        var endpoint = app.MapGroup("").RequireRateLimiting("GeneralPolicy");
        endpoint.MapGet("/health", () => Results.Ok()).RequireAuthorization();
    }
}
