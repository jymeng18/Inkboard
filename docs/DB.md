# Database Migrations

## Adding a new migration

```bash
cd server/Inkboard.Infra/
dotnet ef migrations add <MigrationName> -s ../Inkboard.API
```

`-s` points EF to the startup project (Inkboard.API) so it can resolve the connection string and compile.

## Applying migrations to the database

```bash
cd server/Inkboard.Infra/
dotnet ef database update -s ../Inkboard.API
```

## Connection string

Configured via `ConnectionStrings:WebApiDatabase` in `appsettings.json` or user-secrets.
