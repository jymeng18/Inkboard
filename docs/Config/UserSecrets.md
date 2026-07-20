# User Secrets (Development)

Manage secrets for .NET application.

## Example of Setting a user secret

```bash
dotnet user-secrets set "JwtConfig:Jwt:Key" "your-symmetric-key" --project server/Inkboard.API
dotnet user-secrets set "ConnectionStrings:WebApiDatabase" "Host=localhost;Database=inkboard;Username=postgres;Password=postgres" --project server/Inkboard.API
```

## Listing user secrets

```bash
dotnet user-secrets list --project server/Inkboard.API
```
