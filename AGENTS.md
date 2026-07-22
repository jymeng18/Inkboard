# AGENTS.md — Inkboard
 
## Project Overview
 
Inkboard is a real-time collaborative drawing platform. Up to 5 users share a canvas simultaneously via SignalR WebSockets. Users are grouped into parties with a leader/member role system. The backend is ASP.NET Core following strict Clean Architecture. The frontend is React + TypeScript with Konva.js for canvas rendering.
 
---
 
## Architecture
 
Dependencies flow strictly inward. Never violate this:
 
```
API → Infra → Application → Domain
```
 
| Project | Responsibility |
|---|---|
| `Inkboard.Domain` | Entities, repository interfaces, enums. Zero dependencies. |
| `Inkboard.Application` | Business logic, service interfaces, DTOs, `Result<T>`. Depends only on Domain. |
| `Inkboard.Infra` | EF Core, repository implementations, `AppDbContext`. Depends on Application + Domain. |
| `Inkboard.API` | Minimal API endpoints, SignalR hubs, DI bootstrap. Depends on all layers. |
 
**Hard rules:**
- `Domain` has no references to any other project.
- `Application` never references `Infra` or `API`.
- Services never call `AppDbContext` directly — only through repository interfaces.
- Hubs and endpoints never call repositories directly — only through services.
---
 
## Forbidden Operations
 
- **No `git` commands** of any kind.
- **No file or directory deletions.** If something needs to be removed, leave a `// TODO: remove` comment and explain why.
- Do not run database migrations automatically. Generate them and stop — never apply.
- Do not modify `Program.cs` DI registrations unless explicitly asked.
---
 
## Code Style
 
All code must match the existing style exactly. Use the patterns below as the canonical reference.
 
### Naming
- Fields: `private readonly camelCase` — no underscores, no `_` prefix.
- Methods: `PascalCase`. Parameters and local variables: `camelCase`.
- Async methods always end in `Async`.
### Guard Clauses

```csharp
if (party is null)
    return Result<PartyInvite>.Fail(ErrorType.NotFound, "Party not found.");
 
if (party.LeaderId != leaderId)
    return Result<PartyInvite>.Fail(ErrorType.Forbidden, "Only the leader can invite people.");
```
 
 
### Result Pattern
All service methods return `Result` or `Result<T>`. Never throw exceptions for expected failures. Never return raw domain objects from a service method.
 
```csharp
// Failure
return Result.Fail(ErrorType.NotFound, "Party not found.");
return Result<Party>.Fail(ErrorType.Forbidden, "Only the leader can do this.");
 
// Success
return Result.Ok();
return Result<Party>.Ok(newParty);
```
 
`ErrorType` maps to HTTP status codes — the service never concerns itself with HTTP:
 
| ErrorType | HTTP |
|---|---|
| NotFound | 404 |
| Forbidden | 403 |
| Validation | 400 |
| Conflict | 409 |
| Unexpected | 500 |
 
### Endpoints
Endpoints translate `Result` to HTTP responses only. The pattern is always:
```csharp
var result = await service.DoSomethingAsync(...);
if (!result.IsSuccess)
    return ToErrorResult(result.Error, result.ErrorType);
 
return Results.Ok(result.Data);
```
 
- No try/catch blocks in endpoints.
- No business logic in endpoints — that belongs in the service.
- All endpoints must call `.RequireAuthorization()`.
- User ID is always extracted via `user.GetUserId()`. If it returns `Guid.Empty`, return `Results.Unauthorized()` immediately.
### Project Structure Conventions
- New service methods go in `Inkboard.Application/Services/`.
- New repository interfaces go in `Inkboard.Domain/Repositories/`.
- New repository implementations go in `Inkboard.Infra/Db/`.
- New endpoints go in `Inkboard.API/Routes/`.
- DTOs go in `Inkboard.Application/{Feature}/DTO/`.
---
 
## Testing
 
### Rules
- Tests must test business rules — not implementation details, not whether a method was called.
- Tests must never be written to pass. A test that cannot fail is worthless.
- If a business rule is unclear, stop and ask before writing the test.
- Use real arrange/act/assert structure with clear separation.
- Mock all repository interfaces and `IPartyNotifier` — never use a real DB in unit tests.
- One behaviour per test. Do not assert multiple unrelated things in a single test.
### What to Test
- Every guard clause in every service method is a test case.
- Every happy path is a test case.
- Re-check conditions (e.g. block list re-checked on invite acceptance, membership cap re-checked on invite acceptance) are explicit test cases.
- Leadership transfer logic is a test case.
- Party dissolution when the last member leaves is a test case.
### What Not to Test
- Do not test that EF Core saves an entity — that is an infrastructure concern.
- Do not test that a repository method was called a specific number of times unless the count is itself a business rule.
- Do not test `Result.IsSuccess == true` in isolation — assert on the actual returned data or the downstream effect.
### Naming Convention
```
MethodName_StateUnderTest_ExpectedBehaviour
```
Examples:
```
InviteUserAsync_WhenPartyIsFull_ReturnsValidationFailure
LeavePartyAsync_WhenLeaderIsLastMember_DissolvesParty
RespondToUserInviteAsync_WhenLeaderBlockedUserAfterInvite_ReturnsForbidden
```
 
---
 
## Real-Time Layer
 
- `PartyHub` and `CanvasHub` call services only — never repositories.
- Notification side effects (e.g. `NotifyKick`, `NotifyMemberJoined`) are always called after the database operation succeeds, never before.
- `IPartyNotifier` lives in `Inkboard.Application`. The concrete `PartyNotifier` using `IHubContext` lives in `Inkboard.API`. Never reference `IHubContext` inside Application or Domain.
- Cursor events are fire-and-forget and are never persisted. Do not add persistence logic to cursor handlers.
