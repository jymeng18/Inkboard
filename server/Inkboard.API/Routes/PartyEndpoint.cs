using Inkboard.Application.Interfaces;

namespace Inkboard.API.Routes
{
    public static class PartyEndpoint
    {
        public static void MapPartyEndpoint(this IEndpointRouteBuilder endpoint)
        {
            endpoint.MapPost("/api/parties", async (IPartyService partyService) =>
            {
                // 
            });
        }
    }
}
