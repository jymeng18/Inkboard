# JWT Auth

Project uses JWT Auth to authorize user.

Configuration is setup in Inkboard.Infra/DependencyInjection and it uses the JwtConfig:Jwt:Key that shoud exist in user-secrets. Every REST API endpoint excluding login/register/ all require a **Authorization** header inside the payload.
Standard JWT auth workflow.

Every user has a refresh token that can expire. Refer to Inkboard.Domain/Models/RefreshToken.cs for more information, or check our the ER diagram. 

# JWT Auth in SignalR Hub Connections
In websocket handshakes, the token is passed inside the query params. This configuration/setup can be seen in DepedencyInjection.cs too. 