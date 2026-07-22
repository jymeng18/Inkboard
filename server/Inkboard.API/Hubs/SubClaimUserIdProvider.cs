using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Inkboard.API.Hubs;

public sealed class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        if (connection.User == null)
        {
            return null;
        }

        return connection.User?.FindFirst("sub")?.Value;
    }
}
