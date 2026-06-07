# Backend Architecture Guide (Clean Architecture)

The backend follows **Clean Architecture** (Onion Architecture) to strictly separate concerns, ensuring that the core business rules are independent of external frameworks, databases, and UI (API).

Dependencies must always point **inwards** toward the Domain layer:
`API → Infra → Application → Domain`

---

## Layers Breakdown

### 1. Inkboard.Domain
The absolute center of the application. Contains core business models and repository interfaces. **No external dependencies** (no EF Core, no API frameworks).

*   **Models:** `User`, `Party` (stub), `RefreshToken`
*   **Repository Interfaces:** `IUserRepository`, `ITokenRepository`

---

### 2. Inkboard.Application
Defines the **Use Cases** of the system. Orchestrates business logic but fully abstracts *how* data is stored or where requests come from.

*   **Services:**
    *   `AuthService`: Password hashing, login/register/refresh/logout logic.
    *   `PartyService` *(stub)*: Party creation, membership cap, block list validation.
    *   `CanvasService` *(stub)*: Operation validation, snapshot coordination.
*   **Interfaces:** `IAuthService`, `ITokenGenerator`
*   **Auth sub-module:**
    *   `Auth/DTO/` — `LoginRequestModel`, `RegisterRequestModel`, `RefreshRequestModel`
    *   `Auth/Handlers/` — `RegisterRequestValidator`

---

### 3. Inkboard.Infra (Infrastructure)
Implements interfaces from Application and Domain layers. Knows about external tools: databases, JWT, etc.

*   **Database Access (EF Core):** `AppDbContext`, migrations, `UserRepository`, `TokenRepository`
*   **Auth:** `TokenGenerator` (implements `ITokenGenerator`)
*   **DI:** `DependencyInjection` extension for service registration

---

### 4. Inkboard.API
The entry point. Receives HTTP/WebSocket requests, delegates to Application layer, returns responses. **Strictly no business logic.**

*   **Endpoints (Minimal API):**
    *   `AuthEndpoint`: register, login, refresh, logout
    *   `UserEndpoint`: profile, search users
*   **Real-time Hubs (SignalR) — *(planned)***
    *   `CanvasHub`: canvas sessions, drawing broadcasts, live cursors
    *   `PartyHub`: party invites, join/leave notifications
*   **Configuration:** `Program.cs`, middleware, auth 