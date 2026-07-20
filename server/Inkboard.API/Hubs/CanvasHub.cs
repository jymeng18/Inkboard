using Inkboard.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Inkboard.API.Hubs;

[Authorize]
public class CanvasHub : Hub<ICanvasHubClient>
{
    private readonly IPartyRepository _partyRepository;

    public CanvasHub(IPartyRepository partyRepository)
    {
        _partyRepository = partyRepository;
    }

    public override async Task OnConnectedAsync()
    {
        
    }
}
