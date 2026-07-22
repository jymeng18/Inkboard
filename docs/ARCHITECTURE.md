# Inkboard — System Architecture

## Project Overview

Inkboard is a real-time multi-user collaborative canvas platform, similar to MS Paint but with live multiplayer. Up to 5 users can draw on a shared canvas simultaneously and see each other's actions in real time. Users are organized into parties, similar to Fortnite's party system, where a leader controls membership.

---

## Tech Stack

| Layer            | Technology                                    |
| ---------------- | --------------------------------------------- |
| Backend          | ASP.NET Core Web API                          |
| Real-time        | ASP.NET Core SignalR (WebSockets)             |
| Frontend         | React + TypeScript (Vite)                     |
| Canvas Rendering | Konva.js (react-konva)                        |
| Database         | PostgreSQL via Entity Framework Core (Npgsql) |
| Blob Storage     | Azure Blob Storage                            |
| State Management | Zustand                                       |
| HTTP Client      | Axios + TanStack React Query                  |
| Authentication   | JWT (access token + refresh token)            |
| Styling          | Tailwind CSS                                  |

---

## Project Structure

### Backend — Clean Architecture (4 projects)

The backend follows Clean Architecture with strict dependency inversion. Dependencies point inward: `API → Infra → Application → Domain`.

```
server/
├── Inkboard.Domain/       Core entities and repository interfaces
│   ├── Models/            User, Party, RefreshToken, Canvas, CanvasOperation
│   └── Repositories/      IUserRepository, ITokenRepository, ICanvasRepository
├── Inkboard.Application/  Business logic and use cases
│   ├── Services/          AuthService, PartyService, CanvasService
│   ├── Interfaces/        IAuthService, ITokenGenerator, IBlobStorageService
│   └── Auth/              DTOs, handlers, validators
├── Inkboard.Infra/        EF Core, external service implementations
│   ├── Db/                AppDbContext, repository implementations
│   ├── Auth/              TokenGenerator
│   ├── Blob/              BlobStorageService (Azure SDK implementation)
│   └── Migrations/        EF Core migrations
└── Inkboard.API/          Entry point, minimal API endpoints
    ├── Routes/            AuthEndpoint, UserEndpoint, CanvasEndpoint
    ├── Hubs/              CanvasHub, PartyHub
    └── Program.cs         App bootstrap and DI configuration
```

### Frontend — `client/`

Vite + React + TypeScript project.

```
client/
└── src/
    ├── App.tsx           Root component
    ├── main.tsx          Entry point
    ├── index.css         Global styles
    └── assets/           Static assets
```

---

## Architecture Layers

### REST API (Minimal API Endpoints)

Handles standard HTTP request/response for operations that do not need to be real time. Each endpoint maps to one feature domain.

- **AuthEndpoint** — register, login, refresh token, logout
- **UserEndpoint** — get user profile, search users by username
- **CanvasEndpoint** — upload snapshot, end session

### Real-Time Layer (SignalR Hubs)

Handles all live communication between clients. Two hubs run alongside the REST API.

- **CanvasHub** — manages canvas sessions. Clients join a group by canvas ID. Drawing operations are broadcast to all other members of that group immediately. Cursor positions are broadcast as fire-and-forget (never persisted). Canvas snapshots are sent to a user when they first join a session. When the canvas owner disconnects unexpectedly, the hub detects this in `OnDisconnectedAsync` and notifies all group members to exit the session.
- **PartyHub** — manages party notifications. Delivers real-time invite notifications to target users. Broadcasts member join/leave events to the party.

### Services

Contain all business logic. Controllers and Hubs call services, never the database directly.

- **AuthService** — password hashing, JWT access token generation, refresh token rotation
- **PartyService** — party creation rules, invite validation, membership cap enforcement, block list enforcement
- **CanvasService** — snapshot upload, session termination, operation persistence, canvas ownership enforcement

### Blob Storage Layer

Azure Blob Storage is used to persist canvas snapshots.

- The interface `IBlobStorageService` lives in `Inkboard.Application/Interfaces/`. It has no knowledge of Azure — it is a pure abstraction.
- The concrete implementation `BlobStorageService` lives in `Inkboard.Infra/Blob/` and uses the Azure SDK directly.
- A single blob container named `snapshots` is created once at application startup if it does not already exist.
- Blobs are keyed by canvas ID: `snapshots/{canvasId}.png`.
- `CanvasService` is the only caller of `IBlobStorageService`. The API layer and hubs are never aware of blob storage.

```csharp
public interface IBlobStorageService
{
    Task<string> UploadSnapshotAsync(Guid canvasId, Stream imageData, string contentType);
}
```

`UploadSnapshotAsync` returns the public URL of the uploaded blob so `CanvasService` can update `Canvas.SnapshotUrl` immediately after.

### Data Layer

Entity Framework Core with PostgreSQL (via Npgsql). The `AppDbContext` is the single point of database access.

---

## Database Schema

### Tables

**Users**
Stores registered user accounts.
Fields: Id (Guid), UserName, Email, PasswordHash, CreatedAt

**RefreshTokens**
Stores refresh tokens for JWT auth rotation.
Fields: Id (Guid), TokenHash, ExpiresAt, CreatedAt, IsRevoked, UserId (FK → Users)

**Parties**
Represents a collaboration session group.
Fields: Id, LeaderId (FK → Users), CanvasId (FK → Canvases, nullable), CreatedAt, Status

**PartyMembers**
Junction table linking users to parties with roles.
Fields: PartyId (FK), UserId (FK), Role (Leader | Member), JoinedAt

**PartyInvites**
Tracks pending, accepted, and declined invitations.
Fields: Id, PartyId (FK), InvitedByUserId (FK), InvitedUserId (FK), Status (Pending | Accepted | Declined), ExpiresAt

**BlockList**
Tracks users blocked by a party leader.
Fields: UserId (FK), BlockedUserId (FK), CreatedAt

**Canvases**
Represents a shared drawing canvas.
Fields: Id, OwnerId (FK → Users), SnapshotUrl (nullable), CreatedAt, LastModifiedAt

**CanvasOperations**
Append-only log of every drawing operation during a session.
Fields: Id, CanvasId (FK), UserId (FK), Type, Data (JSON), Timestamp

### Relationships

- A User has many RefreshTokens
- A Party has one Canvas (nullable — a party with no active canvas is a valid state)
- A Party has one Leader (User) and up to 4 additional Members
- A Canvas has one Owner (User) — fixed at creation, never changes
- A Canvas has many CanvasOperations
- A User can have many PartyInvites (sent and received)
- A User can block other Users via BlockList

---

## Canvas Ownership Rules

These rules are fixed and enforced by `CanvasService` on every relevant operation.

- **Canvas ownership is assigned at creation.** `Canvas.OwnerId` is set to creators userId. Inside each canvas, there is a start collab or 'invite' button, only when this is clicked will we initiate a Party, upon creation, Party.CanvasId -> Canvas.Id
- **Canvas ownership cannot be transferred.** There is no operation that changes `Canvas.OwnerId`.
- **Only the canvas owner can end the session.** Any end session request from a non-owner is rejected with `ErrorType.Forbidden`.
- **Only the canvas owner can upload the authoritative snapshot.** Periodic snapshots and the final end-session snapshot both require the requester to be the canvas owner.
- **If the canvas owner transfers party leadership mid-session**, every member is immediately ejected from the canvas session. The party itself remains alive. `Party.CanvasId` is set back to null. Members retain their party membership and can start a new canvas session under the new leader, who becomes the owner of that new canvas.
- **If the canvas owner disconnects unexpectedly** (crash, closed tab, lost connection), `CanvasHub.OnDisconnectedAsync` detects that the disconnecting user is the canvas owner and broadcasts a forced exit event to all members in the canvas group. The session ends with the last periodic snapshot as the final state.

---

## Canvas Session Lifecycle

### Starting a Session

1. Party leader creates a canvas via `POST /api/canvases`.
2. `CanvasService` creates a `Canvas` record with `OwnerId = leaderId` and `SnapshotUrl = null`.
3. `Party.CanvasId` is updated to the new canvas ID.
4. All party members can now connect to the `CanvasHub` group for this canvas.

### During a Session

- Members draw, and operations are broadcast via `CanvasHub` and persisted asynchronously.
- The frontend runs a 15-minute interval timer. On each tick, the canvas owner's client calls `stage.toBlob()` and POSTs the result to `POST /api/canvases/{canvasId}/snapshot`. This is a safety net against unexpected session termination.
- Non-owner clients do not send periodic snapshots.

### Ending a Session (Normal — Owner clicks "End Session")

1. Frontend calls `stage.toBlob()` to render the current canvas state as a PNG.
2. Frontend POSTs the blob to `POST /api/canvases/{canvasId}/snapshot` as `multipart/form-data`.
3. Backend uploads the image to Azure Blob Storage and updates `Canvas.SnapshotUrl`.
4. Frontend POSTs to `POST /api/canvases/{canvasId}/end`.
5. `CanvasService` verifies `OwnerId == requesterId`, then notifies all canvas group members via `ICanvasNotifier` to disconnect.
6. `Party.CanvasId` is set to null. The party remains alive.

### Ending a Session (Leadership Transfer Mid-Session)

1. Canvas owner transfers party leadership via the existing party leadership transfer flow.
2. `PartyService` detects an active canvas on the party (`Party.CanvasId` is not null).
3. `PartyService` calls `CanvasService.ForceEndSessionAsync(canvasId)`.
4. `CanvasService` notifies all canvas group members to disconnect. No final snapshot is taken — the last periodic snapshot is the final state.
5. `Party.CanvasId` is set to null. Party leadership is transferred as normal.

### Ending a Session (Unexpected Owner Disconnect)

1. `CanvasHub.OnDisconnectedAsync` fires for the disconnecting user.
2. Hub checks if the disconnecting user is the canvas owner.
3. If yes, hub calls `CanvasService.ForceEndSessionAsync(canvasId)`.
4. All remaining group members receive a forced exit notification.
5. No final snapshot is taken — the last periodic snapshot is the final state.
6. `Party.CanvasId` is set to null. The party remains alive.

---

## Real-Time Data Flow

### Drawing Sync

1. User draws a stroke on the canvas
2. Frontend renders it locally immediately (optimistic rendering — no waiting for the server)
3. Frontend sends the stroke as a lightweight operation object to the CanvasHub via WebSocket
4. CanvasHub broadcasts the operation to all other clients in the same canvas group
5. Other clients receive the operation and render it on their canvas
6. The operation is persisted to the database asynchronously — it does not block the broadcast

### Cursor Sync

1. User moves their mouse on the canvas
2. Frontend sends cursor position to CanvasHub at a throttled rate (approximately 30 events per second)
3. CanvasHub broadcasts cursor position and userId to all other clients in the group
4. Other clients render a labelled cursor for that user
5. Cursor events are never persisted to the database

### Canvas Snapshot Upload

1. Triggered either by the 15-minute frontend interval timer or by the owner clicking "End Session"
2. Frontend calls `stage.toBlob()` on the Konva stage to render the current canvas as a PNG blob
3. Frontend POSTs the blob to `POST /api/canvases/{canvasId}/snapshot` as `multipart/form-data`
4. `CanvasService` verifies the requester is the canvas owner
5. `CanvasService` calls `IBlobStorageService.UploadSnapshotAsync`, which uploads the image to the `snapshots` Azure Blob container under the key `{canvasId}.png`
6. `CanvasService` updates `Canvas.SnapshotUrl` with the returned blob URL
7. When a new user joins an existing session, they receive the latest `SnapshotUrl` immediately so they see the current canvas state

---

## Party System

Modelled after Fortnite's party system.

### Roles

- **Leader** — the user who created the party. Has full control over membership.
- **Member** — a user who accepted an invite. Can draw on the canvas and leave the party.

### Leader Capabilities

- Invite a user by username
- Remove a member from the party
- Block a user (blocked users cannot be invited again)
- Transfer leadership (see canvas session impact above)
- Start a canvas session (creates a new Canvas, becomes its owner)

### Membership Rules

- Maximum 5 users per party (1 leader + 4 members)
- A user cannot be invited if they are on the leader's block list
- An invite expires after 5 minutes if not accepted
- When the leader leaves, leadership transfers to the longest-standing member
- If a canvas session is active when leadership transfers, the session is force-ended and all members are ejected from the canvas (party membership is unaffected)
- When a member leaves, their cursor is removed from all other clients in real time via the PartyHub

### Invite Flow

1. Leader sends an invite via the PartyController REST endpoint
2. A PartyInvite record is created in the database with status Pending and a 10-minute expiry
3. PartyHub delivers a real-time notification to the target user if they are online
4. Target user accepts or declines via the REST endpoint
5. On accept, a PartyMember record is created and the user is connected to the CanvasHub group
6. On decline or expiry, the invite is marked accordingly and no action is taken

---

## Authentication

### Token Strategy

- **Access Token** — JWT, short-lived (15 minutes). Sent in the Authorization header on every REST request.
- **Refresh Token** — long-lived (7 days), stored securely. Used to obtain a new access token without re-login.
- Tokens are validated on every REST request and every SignalR connection.
- SignalR receives the access token via the query string on the WebSocket handshake URL, since browsers cannot set custom headers on WebSocket connections.

### Auth Flow

1. User registers or logs in via the AuthEndpoint
2. AuthService returns an access token and a refresh token
3. Frontend stores the access token in memory (not localStorage) and the refresh token in an httpOnly cookie
4. Axios intercepts 401 responses, calls the refresh endpoint, and retries the original request
5. All SignalR hub methods are protected and reject unauthenticated connections

---

## Redis Usage

Redis serves two purposes.

**SignalR Backplane**
If the backend runs on more than one server instance, SignalR uses Redis to fan out messages across all instances so every client receives broadcasts regardless of which server they are connected to.

**Session and Party State Cache**
Frequently read data is cached in Redis to avoid hitting PostgreSQL on every real-time event. This includes current party membership, canvas metadata, and active user connection IDs.

---

## Latency Minimization Strategy

The following design decisions exist specifically to minimize the delay users experience when seeing each other's actions.

- WebSockets via SignalR are used for all real-time events. REST is never used for drawing or cursor data.
- Drawing is rendered locally on the sender's canvas before the server confirms anything (optimistic rendering).
- Only the delta (the new stroke or cursor position) is sent over the wire, never the full canvas state.
- Database persistence of operations is done asynchronously and does not block the broadcast to other clients.
- Cursor events are throttled on the frontend before being sent, and are never acknowledged by the server.
- Snapshot uploads are fire-and-forget from the frontend's perspective during periodic saves — only the end session snapshot waits for confirmation before proceeding to end the session.
- (Planned) Redis caches session state so the server does not query PostgreSQL during real-time hub calls.

---

## Canvas Rendering Architecture (Frontend)

Two canvas layers are stacked on top of each other using Konva.js.

- **Bottom layer (Snapshot Layer)** — displays the last committed canvas state. Updated only when a new snapshot is received.
- **Top layer (Drawing Layer)** — displays active in-progress strokes from the current user and other users. Strokes are merged down to the snapshot layer when a session ends or a snapshot is saved.

This two-layer approach prevents React state re-renders from slowing down active drawing.

---

## Conflict Resolution

When two users draw on the same area simultaneously, the strategy is Last Write Wins with deterministic ordering.

- Every operation has a timestamp and a userId
- Operations are applied in timestamp order
- If two operations share the same timestamp, they are ordered by userId as a tiebreaker
- This is acceptable for a paint-style canvas where pixel-level conflicts are visually tolerable

---

## Build Order (Recommended)

This is the recommended sequence for implementing features, designed to validate the hardest parts early.

1. Auth — register, login, JWT issuance and validation
2. Basic single-user canvas — drawing works locally with Konva.js
3. SignalR canvas sync — two users can draw together in real time
4. Party system — invite, join, leave via REST with SignalR notifications
5. Party controls — remove, block, leadership transfer
6. Canvas session lifecycle — start session, end session, force end on leadership transfer and unexpected disconnect
7. Canvas persistence — periodic snapshots and end-session snapshots uploaded to Azure Blob Storage
8. Polish — reconnection handling, undo/redo, user cursors with name labels, offline state indicators
