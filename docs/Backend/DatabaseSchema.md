# Database Schema

PostgreSQL via Entity Framework Core (Npgsql). `AppDbContext` is the single point of database access.

## Tables

### Users
Registered accounts.
Fields: Id, UserName, Email, PasswordHash, CreatedAt.
Email has a unique index.

### RefreshTokens
Refresh tokens for JWT rotation.
Fields: Id, TokenHash, ExpiresAt, CreatedAt, IsRevoked, UserId (FK to Users, cascade).
TokenHash has a unique index.

### Parties
A collaboration session group.
Fields: Id, LeaderId (FK to Users, restrict), CanvasId (FK to Canvases, nullable, set null), CreatedAt.

### PartyMembers
Junction table linking users to parties.
Fields: PartyId (FK, cascade), UserId (FK, cascade), Role (Leader or Member), JoinedAt.
Composite primary key (PartyId, UserId).

### PartyInvites
Tracks invitations sent to join a party.
Fields: Id, PartyId (FK, cascade), InvitedByUserId (FK, cascade), InvitedUserId (FK, cascade), InviteStatus (Pending, Accepted, Declined, Expired), CreatedAt, ExpiresAt.
Unique index on (PartyId, InvitedUserId) filtered to InviteStatus = Pending, so only one live invite per user per party can exist at a time.

### BlockList
Tracks users blocked by another user.
Fields: UserId (FK, cascade), BlockedUserId (FK, cascade), CreatedAt.
Composite primary key (BlockedUserId, UserId).

### Canvases
A shared drawing canvas.
Fields: Id, OwnerId (FK to Users, cascade), Name, SnapshotURL (nullable), CreatedAt, LastModifiedAt.

### CanvasOperations
Append only log of drawing operations.
Fields: Id, CanvasId (FK, cascade), UserId (FK to Users, nullable, set null), Type, OperationData (jsonb), Timestamp.
UserId is nullable and set null on account deletion, so a stroke on someone else's canvas survives the author's account being removed. It only shows up as attribution missing, never as a deleted stroke.
Index on (CanvasId, Timestamp) for replaying a canvas in order.

### Friend_Requests
Tracks a pending, accepted, declined, or revoked request between two users.
Fields: Id, RequesterId (FK to Users, cascade), RequesteeId (FK to Users, cascade), Status, CreatedAt.
Unique index on (RequesterId, RequesteeId) filtered to Status = Pending, so the same sender cannot stack duplicate pending requests to the same receiver.
Index on RequesteeId for reading a user's inbox.

### Friendships
One row per confirmed friend pair.
Fields: UserId1, UserId2, CreatedAt.
Composite primary key (UserId1, UserId2).
Check constraint requires UserId1 < UserId2 by raw uuid byte order, so a pair can only ever be stored once regardless of which user initiated it. The repository is responsible for sorting a pair into this order before every read and write.
Index on UserId2, since the primary key only indexes UserId1 and a user can land on either side of a row.

## Delete behavior

Rows that are pure associations between two users cascade when either user is deleted: PartyMembers, PartyInvites, BlockList, FriendRequests, Friendships, RefreshTokens.

Rows that are content, or that another user still depends on, do not cascade:
* `Party.LeaderId` is restrict. Deleting a party leader should go through the leadership transfer flow, not silently dissolve a live party.
* `CanvasOperation.UserId` is set null rather than cascade or restrict, for the reason described above.
* `Canvas.OwnerId` is cascade. Deleting the owner also removes the canvas and everything drawn on it, since nothing depends on that canvas surviving once its owner is gone.
