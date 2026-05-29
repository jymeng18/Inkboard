# Inkboard — agent guidance

## Structure

Two-package monorepo:

- **`client/`** — React 19 + Vite 8 + TypeScript 6 frontend  
  Entry: `src/main.tsx`  
  Canvas via Konva/react-konva, real-time via SignalR (@microsoft/signalr), state via Zustand, data fetching via TanStack Query + Axios

- **`server/`** — .NET 10 ASP.NET Core Web API  
  Solution: `Inkboard.slnx` (new `.slnx` format, not legacy `.sln`)  
  Clean Architecture:
  - `Inkboard.API` — controllers, Program.cs (port 8000 in dev)
  - `Inkboard.Application` — services, auth (TokenGenerator)
  - `Inkboard.Domain` — models (User)
  - `Inkboard.Infra` — EF Core DbContext, PostgreSQL via Npgsql, migrations

  Layering: API → Application → Domain; API → Infra → Application → Domain  
  JWT auth with symmetric key from `JwtConfig:Jwt:Key` (config via user-secrets or appsettings)  
  No hosted SignalR hub yet — SignalR dependency is installed but unused.

## Key commands

### Client (`client/`)
```sh
npm run dev      # Vite dev server (default :5173)
npm run build    # tsc -b && vite build  (typecheck == build prerequisite)
npm run lint     # eslint .
```

### Server (`server/`)
```sh
dotnet run --project Inkboard.API     # start API on http://localhost:8000
dotnet build Inkboard.slnx             # build entire solution
dotnet watch --project Inkboard.API    # hot reload
dotnet ef migrations add <name>        # add EF migration
```

No `dotnet format` or analyzers enforced in the project files — lint is manual.

## Current state (early development)

- Both controllers (`AuthController.cs`, `UserController.cs`) and most service files are empty stubs.  
- Only `User` domain model, `AppDbContext`, and `TokenGenerator` have real code.  
- `Party.cs`, `CanvasService.cs`, `PartyService.cs`, `AuthService.cs` are empty stubs.  
- No tests exist anywhere in the repo.  
- Only CI is Dependabot for NuGet (weekly, Monday).  
- Tailwind CSS v4 is in `devDependencies` but not wired into any CSS file yet.  
- React Compiler Babel plugin is configured in `vite.config.ts`.

## Database

PostgreSQL via EF Core. Connection string: `ConnectionStrings:WebApiDatabase` in appsettings or user-secrets.  
Existing migration: `20260526042549_InitialCreate` (creates `Users` table).

## Conventions

- `"verbatimModuleSyntax": true` in tsconfig — use `import type` for type-only imports.
- `"erasableSyntaxOnly": true` — no enums, no namespaces; use `const` objects or unions.
- No test infrastructure set up yet — add test projects under `server/` or a `client/__tests__/` directory as needed.
- When adding new API endpoints, register them in `Program.cs` (currently using minimal API style).

## Permissions

- You are not allowed to run any commands without my permission first, do not run or suggest any Git commands, this will be  handled by me. 
