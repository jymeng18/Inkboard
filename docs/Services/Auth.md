# JWT Auth

Project uses JWT Auth to authorize user.

Configuration is setup in Inkboard.Infra/DependencyInjection and it uses the JwtConfig:Jwt:Key that shoud exist in user-secrets. Every REST API endpoint excluding login/register/ all require a **Authorization** header inside the payload.
Standard JWT auth workflow.

Every user has a refresh token that can expire. Refer to Inkboard.Domain/Models/RefreshToken.cs for more information, or check our the ER diagram. 

## Token lifetimes
* Access token, 60 minutes.
* Refresh token, 7 days.

## Auth flow
1. Register creates the account but returns no tokens. The client logs in right after with the same credentials to get a session.
2. Login returns an access token and a refresh token.
3. The client attaches the access token as a Bearer header on every request.
4. When the access token expires, the client calls the refresh endpoint with the refresh token to get a new pair.
5. Logout revokes the refresh token server side.

# JWT Auth in SignalR Hub Connections
In websocket handshakes, the token is passed inside the query params. This configuration/setup can be seen in DepedencyInjection.cs too. 