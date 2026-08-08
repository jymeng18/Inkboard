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

### REST API

Standard HTTP request/response for anything that isn't real-time — auth, user profile/search, and canvas management including snapshot upload and session end. Each route belongs to one feature domain.

### Real-Time Layer (SignalR Hubs)

All live communication runs over SignalR, alongside the REST API. Two concerns are kept on separate hubs:

- **Canvas sessions** — clients group by canvas id; drawing operations broadcast to the rest of the group immediately, cursor positions broadcast fire-and-forget (never persisted). An unexpected owner disconnect is detected here and force-ends the session for the group.
- **Party notifications** — real-time invite delivery and member join/leave broadcasts.

**Decision: real-time and REST are deliberately split.** Drawing and cursor data never touch REST; request/response operations never touch a hub. This keeps the latency-sensitive path off HTTP entirely.

### Services

All business logic lives in services; the API and hubs call services, never the database. The boundaries: auth (hashing, token issuance and rotation), party (membership rules, invite validation, caps, block list), and canvas (snapshot handling, session termination, ownership enforcement).

### Blob Storage Layer

Canvas snapshots are persisted to Azure Blob Storage, behind an abstraction: the application layer depends on a storage interface with no knowledge of Azure, and the Azure implementation lives in infrastructure. The canvas service is the only caller — the API and hubs are never aware of blob storage.

The container is **private**, one blob per canvas, overwritten each snapshot. Because a private container can't be read by a credential-less browser, **reads are proxied through the server** (which authenticates to Azure with Entra ID/RBAC) rather than via SAS tokens. The full rationale, storage shape, join catch-up, and upload validation are in `Services/AzureBlobStorage.md`.

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
Fields: Id, OwnerId (FK → Users), Name (nullable, max 50), SnapshotURL (nullable), SnapshotTakenAt (nullable), CreatedAt, LastModifiedAt

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

## Canvas Ownership and Sessions

**Ownership is fixed at creation and never transfers.** That single rule anchors the session model: only the owner ends a session or uploads the authoritative snapshot. A canvas is created first; a party (and the party↔canvas link) is initiated only when the owner starts collaborating.

Sessions end three ways, and the deliberate distinction is whether a final snapshot is taken: an owner ending deliberately captures one first; a leadership transfer or an unexpected owner disconnect force-ends with no final snapshot, letting the last periodic one stand. In all cases the party survives — only the canvas link is severed. Full lifecycle reasoning is in `Workflows/CanvasSessionLifecycle.md`; snapshot storage and join catch-up in `Services/AzureBlobStorage.md`.

---

## Real-Time Data Flow

The latency-critical decisions (optimistic local rendering, delta-only messages, async persistence that never blocks a broadcast, throttled never-persisted cursors) are covered under Latency Minimization below and in the lifecycle doc. Two paths run over the canvas hub:

- **Drawing** — rendered locally on sight, sent as a lightweight delta, broadcast to the group, persisted asynchronously.
- **Cursors** — throttled client-side, broadcast to the group, never persisted.

Snapshots are the non-real-time path: uploaded over REST by the owner's client (periodically and on end), stored to blob, and served back on join via the server-proxied read. See `Services/AzureBlobStorage.md`.

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
