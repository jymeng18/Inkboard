# User Secrets (Development)

Manage secrets for the API project (Inkboard.API).

## Setting a secret

```bash
dotnet user-secrets set "JwtConfig:Jwt:Key" "your-symmetric-key" --project server/Inkboard.API
dotnet user-secrets set "ConnectionStrings:WebApiDatabase" "Host=localhost;Database=inkboard;Username=postgres;Password=postgres" --project server/Inkboard.API
```

## Listing secrets

```bash
dotnet user-secrets list --project server/Inkboard.API
```
