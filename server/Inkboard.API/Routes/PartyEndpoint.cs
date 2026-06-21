using System.Security.Claims;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;

namespace Inkboard.API.Routes
{
    public static class PartyEndpoint
    {
        public static void MapPartyEndpoint(this IEndpointRouteBuilder endpoint)
        {
            endpoint.MapPost("/api/parties", async (IPartyService partyService, ClaimsPrincipal user) =>
            {

                // pulling out a userId, 
                var leaderIdStr = user.FindFirst("sub")?.Value;
                if(!Guid.TryParse(leaderIdStr, out var leaderId)){
                    return Results.Unauthorized();
                }

                // TODO: IPartyService.cs is not implemented yet
                var party = await partyService.CreatePartyAsync(leaderId);


                return Results.Created($"/api/parties/{party.Id}", party);
            });


        }
    }
}
