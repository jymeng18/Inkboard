# Party System

Inkboard organises users into Parties (up to 5 people) for collaborative canvas sessions. Modelled after Fortnite's and most co-op games' party system — a leader controls membership, invites expire, and leadership transfers automatically when the leader leaves.

---

## Domain Models

```
┌─────────────────────┐     ┌──────────────────────────┐
│       Party         │     │      PartyMember         │
├─────────────────────┤     ├──────────────────────────┤
│ Id (Guid) [PK]      │     │ PartyId (Guid) [PK, FK]  │
│ LeaderId (Guid) [FK]│     │ UserId (Guid) [PK, FK]   │
│ CanvasId (Guid?)    │     │ Role (Leader | Member)   │
│ CreatedAt (DateTime)│     │ JoinedAt (DateTime)      │
└─────────────────────┘     └──────────────────────────┘

┌──────────────────────┐    ┌──────────────────────────┐
│     PartyInvite      │    │        BlockList         │
├──────────────────────┤    ├──────────────────────────┤
│ Id (Guid) [PK]       │    │ UserId (Guid) [PK, FK]   │
│ PartyId (Guid) [FK]  │    │ BlockedUserId (Guid)[FK] │
│ InvitedByUserId (FK) │    │ CreatedAt (DateTime)     │
│ InvitedUserId (FK)   │    └──────────────────────────┘
│ InviteStatus (enum)  │
│ CreatedAt (DateTime) │
│ ExpiresAt (DateTime) │
└──────────────────────┘

Party.LeaderId Ref. PartyMember.UserId
Party.CanvasId Ref. Canvas.Id

PartyInvite.InvitedUserId Ref. User.Id
PartyInvite.InvitedByUserId Ref. User.Id
PartyInvite.PartyId Ref. Party.id

BlockList.UserId Ref. User.Id
BlockList.BlockedUserId Ref. User.Id

```

### Party

Each party has exactly one leader. 

### PartyMember 

Junction table with a composite primary key `(PartyId, UserId)`. Two roles:

Leader: User who created party
Member: User who was invited to join the party

### PartyInvite 

Tracks the lifecycle of an invitation. Four statuses:
- Pending
- Accepted
- Declined
- Expired

Invites **expire 5 minutes** after creation. The DB status is only flipped to
`Expired` lazily (e.g. when the same user is re-invited); a still-`Pending` row
whose `ExpiresAt` has already passed is treated as expired by readers.

### BlockList

A user can block another user. Blocked users cannot be invited to the leader's party, and cannot accept pending invites from that leader. Composite key `(BlockedUserId, UserId)`.

---

## Business Rules

### Membership
- **Maximum 5 users per party** (1 leader + 4 members).
- A user cannot be invited if they are already a member.
- A user cannot be invited if the leader has them on their block list.
- Only the leader can invite new users.

### Invite Flow
1. Leader calls `POST /api/parties/{partyId}/invites`.
2. Server creates a `PartyInvite` with status `Pending` and a 5-minute expiry.
3. Invited user responds via `POST /api/invites/{inviteId}/respond`.
4. On accept → a `PartyMember` record is created with role `Member`.
5. On decline → invite status set to `Declined`.
6. A second attempt on a non-pending invite is rejected.
7. Expired invites cannot be accepted.

### Re-validation at Response Time
When a user accepts, the server re-checks:
- Is the invitee on the leader's block list now? (leader may have blocked them since the invite)
- Is the party still under the 5-member cap? (members may have joined since the invite)

### Fetching Pending Invites
- `GET /api/invites` returns every **Pending** invite addressed to the caller (`InvitedUserId == currentUser`). It filters by status only, **not** by time, so a row whose `ExpiresAt` has already passed can still come back — clients must drop expired invites themselves.

### Leadership
- The party creator is the initial leader.
- **Leader leaves a party of 3+** → leadership transfers to the oldest remaining member (earliest `JoinedAt` among `Member`-role members); the canvas link is severed but the party lives on.
- **Any leave that would leave fewer than 2 members** → the party is dissolved (party + member records deleted). This covers a sole member leaving, a leader leaving a 2-person party, and a member leaving a 2-person party — a one-person party is never left behind.
- **Non-leader leaves a party of 3+** → just removed from the party, party continues.

### Kick
- Only the leader can kick a member.
- The leader cannot kick themselves (must use leave/transfer).

### Block
- A user can block another user.
- Cannot block yourself.
- Cannot block someone already blocked.

---

## Invite Delivery & Inbox (Frontend)

An invite reaches the invitee over **two channels that feed one cache**, so it shows up instantly *and* survives a refresh.

- **Realtime push (live path).** The backend creates the `PartyInvite` row, then pushes it over SignalR (`ReceiveInvite`). The client writes it straight into the React Query cache under `['party-invites']` (deduped by id) and pops a 15-second toast. Because the invite is already in hand from the push, the Inbox shows it the same instant — no fetch, no waiting on a poll.
- **Durable pull (backfill).** `GET /api/invites` re-hydrates that same cache. It runs event-driven — on mount, window focus, and hub reconnect — with **no polling interval**. Its only jobs are surviving a page refresh (the in-memory cache is wiped) and catching invites that arrived while the socket was down.

`staleTime` gates only whether one of those events actually hits the network; it never gates what is displayed.

### Inbox
- A dedicated **Party invites** tab lists pending invites straight from the cache. Each row shows the inviter's short id and a static **Due `<time>`** stamp (no live countdown).
- **Expiry is client-side.** Since `GET /api/invites` filters by status only, the tab (and the unread badge) drop invites whose `ExpiresAt` has passed via an `expiresAt > now` filter at render time. There is no history for invites — they simply disappear when answered or expired.
- The **unread badge** counts pending, non-expired invites alongside friend requests.

### Answering
- Accept / Decline from the toast **or** the inbox both go through one shared mutation (`useRespondToInvite`). It optimistically removes the row from the cache and dismisses the toast (matched by a stable per-invite id), so answering in one place clears it everywhere.
- **Accept** hydrates the party store and navigates into the party's canvas; **Decline** just drops the row.

The 15-second toast is therefore only a convenience heads-up: the invite stays actionable in the Inbox for its full 5-minute lifetime.

