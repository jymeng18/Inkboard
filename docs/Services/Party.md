# Party System

Inkboard organises users into Parties (up to 5 people) for collaborative canvas sessions. Modelled after Fortnite's and most co-op games' party system — a leader controls membership, invites expire, and leadership transfers automatically when the leader leaves.

---

## Domain Models

Four tables back the party system: Party, PartyMember (a junction), PartyInvite, and BlockList. Field-level schema and foreign keys live in `../Backend/DatabaseSchema.md`; what matters here are the modelling decisions.

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
- A leader invite creates a Pending invite with a 5-minute expiry.
- The invited user accepts (→ a Member record is created) or declines (→ status Declined).
- A second attempt on a non-pending invite is rejected, and expired invites cannot be accepted — an invite is answered exactly once.

### Re-validation at Response Time
When a user accepts, the server re-checks:
- Is the invitee on the leader's block list now? (leader may have blocked them since the invite)
- Is the party still under the 5-member cap? (members may have joined since the invite)

### Fetching Pending Invites
- Fetching pending invites for a user filters by **status only, not by time**, so a row whose expiry has already passed can still come back. This is a deliberate decision: the server treats a still-Pending-but-past-expiry row as expired lazily, and clients are responsible for dropping expired invites themselves.

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

- **Realtime push (live path).** The backend creates the invite, then pushes it over SignalR. The client writes it straight into the client-side cache (deduped by id) and pops a 15-second toast, so the Inbox shows it the same instant — no fetch, no waiting on a poll.
- **Durable pull (backfill).** A fetch re-hydrates that same cache, **event-driven** (on mount, window focus, hub reconnect) with **no polling interval**. Its only jobs are surviving a page refresh (the in-memory cache is wiped) and catching invites that arrived while the socket was down.

The decision worth remembering: the two channels write one cache, and cache freshness gates only whether an event hits the network, never what is displayed.

### Inbox
- A dedicated **Party invites** tab lists pending invites straight from the cache. Each row shows the inviter's short id and a static **Due `<time>`** stamp (no live countdown).
- **Expiry is client-side.** Because the fetch filters by status only, the tab (and the unread badge) drop past-expiry invites at render time. There is no history for invites — they simply disappear when answered or expired.
- The **unread badge** counts pending, non-expired invites alongside friend requests.

### Answering
- Accept / Decline from the toast **or** the inbox both go through **one shared mutation**. It optimistically removes the row from the cache and dismisses the toast (matched by a stable per-invite id), so answering in one place clears it everywhere — this single-path decision is what keeps toast and inbox from drifting out of sync.
- **Accept** hydrates the party store and navigates into the party's canvas; **Decline** just drops the row.

The 15-second toast is therefore only a convenience heads-up: the invite stays actionable in the Inbox for its full 5-minute lifetime.

