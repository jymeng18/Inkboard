## SignalR WebSocketFactory bypasses AccessTokenProvider

When testing SignalR hubs through TestServer, AccessTokenProvider only feeds two paths: HTTP transport headers (negotiate/long-polling/SSE) and the default ClientWebSocket connection, where the client sets the Authorization header for you.
Setting a custom options.WebSocketFactory (required to route the socket through TestServer) replaces that default path entirely. The client no longer manages the socket, so AccessTokenProvider is never called for it —-> no header, no query param. This is documented client behavior, not a bug: auth becomes the factory's responsibility once you override it. Note also that there's no access_token query-string fallback in the .NET client (that only exists in the JS client, due to browser WebSocket header restrictions).

## Issue & Fix
Issue: WebSocketFactory receives the URI with only the negotiate connection ID (?id=xxx), no access_token, and the JWT middleware never authenticates the connection.
Fix: manually append the token to the URI inside WebSocketFactory:

```csharp
csharpoptions.WebSocketFactory = (context, ct) =>
{
    var uriWithToken = QueryHelpers.AddQueryString(
        context.Uri.ToString(),
        "access_token",
        accessToken
    );

    return new ValueTask<WebSocket>(
        _factory.Server.CreateWebSocketClient()
            .ConnectAsync(new Uri(uriWithToken), ct)
    );
};
```