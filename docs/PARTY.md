# Party System

Inkboard organises users into **parties** (up to 5 people) for collaborative canvas sessions. Modelled after Fortnite's party system — a leader controls membership, invites expire, and leadership transfers automatically when the leader leaves.

---

## Domain Models

```
┌─────────────────────┐     ┌──────────────────────────┐
│       Party         │     │      PartyMember         │
├─────────────────────┤     ├──────────────────────────┤
│ Id (Guid) [PK]      │     │ PartyId (Guid) [PK, FK]  │
│ LeaderId (Guid) [FK]│────>│ UserId (Guid) [PK, FK]   │
│ CanvasId (Guid?)    │     │ Role (Leader | Member)   │
│ CreatedAt (DateTime)│     │ JoinedAt (DateTime)      │
└─────────────────────┘     └──────────────────────────┘

┌──────────────────────┐    ┌──────────────────────────┐
│     PartyInvite      │    │        BlockList         │
├──────────────────────┤    ├──────────────────────────┤
│ Id (Guid) [PK]       │    │ UserId (Guid) [PK, FK]   │
│ PartyId (Guid) [FK]  │    │ BlockedUserId (Guid)[PK] │
│ InvitedByUserId (FK) │    │ CreatedAt (DateTime)     │
│ InvitedUserId (FK)   │    └──────────────────────────┘
│ InviteStatus (enum)  │
│ CreatedAt (DateTime) │
│ ExpiresAt (DateTime) │
└──────────────────────┘
```

### Party

Each party has exactly one leader. 

### PartyMember 

Junction table with a composite primary key `(PartyId, UserId)`. Two roles:

| Role | Description |
|------|-------------|
| `Leader` | Created the party. Full control over membership. |
| `Member` | Accepted an invite. Can draw and leave. |

### PartyInvite 

Tracks the lifecycle of an invitation. Three statuses:

| Status | Meaning |
|--------|---------|
| `Pending` | Awaiting a response from the invited user. |
| `Accepted` | Invited user joined the party. |
| `Declined` | Invited user declined. |

Invites **expire 5 minutes** after creation

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

### Leadership
- The party creator is the initial leader.
- **Leader leaves with members** → leadership transfers to the oldest remaining member (earliest `JoinedAt` among `Member`-role members).
- **Sole member leaves** → party is dissolved (party + member records deleted).
- **Non-leader leaves** → just removed from the party, party continues.

### Kick
- Only the leader can kick a member.
- The leader cannot kick themselves (must use leave/transfer).

### Block
- A user can block another user.
- Cannot block yourself.
- Cannot block someone already blocked.

