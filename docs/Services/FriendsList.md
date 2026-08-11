# Friends List

Two people become friends by request and acceptance, then show up on each other's friends list. Blocking is a separate feature, not yet implemented, and does not gate any rule below.

---

## Domain Models

Two tables: FriendRequest (the request lifecycle) and Friendship (a confirmed pair). Field-level schema lives in `../Backend/DatabaseSchema.md`; the modelling decisions are below.

### FriendRequest
Tracks the lifecycle of one request from one user to another. Four statuses:
* Pending
* Accepted
* Declined
* Revoked

There is no expiry. A request sits Pending until the receiver answers or the sender cancels it.

### Friendship
One row per confirmed pair, no direction, no roles. Composite primary key `(UserId1, UserId2)`.

The pair is always stored smaller id first, `UserId1 < UserId2` by raw uuid byte order, enforced by a check constraint. This means a friendship between A and B can only ever exist as one row, never as both `(A, B)` and `(B, A)`. The repository is what sorts a pair into this order, callers can pass either id first.

---

## Business Rules

### Sending a request
* Cannot send to yourself.
* Cannot send to a user who does not exist.
* Cannot send to someone you are already friends with.
* Cannot send if you already have a pending request to them.
* Cannot send if they already have a pending request to you. The correct action there is to accept or reject theirs, not to file a second one.

### Accepting a request
* The request must exist and be Pending. Declined, Accepted, and Revoked requests are all rejected the same way, they have already been answered.
* The caller must be the receiver named on that specific request, and the sender argument must match the requester named on it. Mismatch on either side returns Forbidden.
* You cannot accept into a friendship that already exists.
* On success, the request is marked Accepted and a `Friendship` row is created.

### Rejecting a request
* Same existence, status, and identity checks as accepting.
* On success, the request is marked Declined. No `Friendship` row is created.

### Cancelling a request
* Only the original sender can cancel, checked against `RequesterId`.
* The request must still be Pending. Anything already answered cannot be cancelled.
* On success, the request is marked Revoked.

### Unfriending
* The other user must exist.
* A `Friendship` row between the two must exist.
* On success, the row is deleted. Nothing stops either side from sending a new request afterward.

### Reading the friends list
Returns the other person on every `Friendship` row the caller appears in, from either column.

### Reading requests
Two views exist:
* Pending only, for an inbox of requests still waiting on an answer.
* All requests, any status, for a full history including ones already answered.

Both return the other user's id and name relative to the caller, never a raw request row.
