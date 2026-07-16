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

Tracks the lifecycle of an invitation. Three statuses:
- Pending
- Accepted
- Declined

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

