namespace Inkboard.API.Hubs;

/// <summary>
/// ConnectionStore uses a ConcurrentDict<key, string> 
/// CanvasNotifier, PartyNotifier both need to store connId's, to avoid conflicts we use a composite key
/// public record class is used because it uses value equality 
/// </summary>
public record class HubUserKey(Guid UserId, string HubName);

