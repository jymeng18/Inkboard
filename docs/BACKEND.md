# Backend Architecture Guide (Onion Architecture)

The backend follows **Onion Architecture** (also known as Clean Architecture) to strictly separate concerns, ensuring that the core business rules are independent of external frameworks, databases, and UI (API). 

Dependencies must always point **inwards** toward the Domain layer:
`API Layer -> Infrastructure -> Application -> Domain`

---

##  Layers Breakdown & Feature Grouping

### 1. Inkboard.Domain
The absolute center of the application. It contains the core business models and defines the rules of the system. **No external dependencies** (no EF Core, no API frameworks).

*   **Entities / Models:** `User`, `Party`, `Canvas`, `PartyMember`, `BlockedUser`
*   **Repository Interfaces:** `IUserRepository`, `IPartyRepository`, `ICanvasRepository` (Interfaces only, no real DB code)
*   **Domain Enums/Constants:** `PartyRole` (Leader, Member), `MaxPartySize = 5`

---

### 2. Inkboard.Application
Defines the **Use Cases** of the system. It controls the flow of data, contains business logic, and orchestrates operations, but fully abstracts *how* data is stored or where requests come from.

*   **Services (Business Logic):**
    *   `AuthService`: Password hashing validation, login/register logic.
    *   `PartyService`: Party creation rules, membership cap enforcement (max 5), block list validation.
    *   `CanvasService`: Operation validation, snapshot logic coordination.
*   **Interfaces for External Tools:** `ITokenGenerator`, `IBlobStorageService`, `ICanvasHubContext` (to let the app layer trigger real-time events without depending on SignalR).
*   **DTOs / ViewModels:** `UserDto`, `PartyInviteRequest`, `AuthResponse`

---

### 3. Inkboard.Infra (Infrastructure)
Implements all the interfaces defined in the application and domain layers. This layer knows about external tools: databases, email providers, file storage, Redis, etc.

*   **Database Access (EF Core):** `InkboardDbContext`, DB Migrations.
*   **Repository Implementations:** `UserRepository`, `PartyRepository` (SQL queries/EF Core logic).
*   **External Service Integrations:** 
    *   `JwtTokenGenerator` (implements `ITokenGenerator`).
    *   `AzureBlobStorageService` (for Canvas snapshots).
    *   `RedisCacheService` (for caching/sessions).

---

### 4. Inkboard.API
The entry point of the backend. It receives external HTTP/WebSocket requests, maps them to DTOs, hands them over to the Application layer, and maps the results to HTTP responses. **Strictly no business logic.**

*   **REST Controllers:** 
    *   `AuthEndpoint`: Endpoints for login/register/refresh mechanisms.
    *   `UserEndpoint`: Endpoints for fetching profiles or searching users.
    *   `PartyController`: Endpoints for creating parties, invites, and kicks.
    *   `CanvasController`: Endpoints for fetching snapshots or metadata.
*   **Real-time Hubs (SignalR):**
    *   `CanvasHub`: Manages group sessions (by canvas ID), drawing broadcasts, and live cursor tracking.
    *   `PartyHub`: Manages real-time party notifications (invites, joins, leaves).
*   **Configuration & DI:** `Program.cs`, middleware, authentication wrappers. 