# UserWorkflow - Canvas Creation

On client side, we don't see an **Invite** button until user creates a **Canvas** first. The party is only created when
the first invite to a user is sent. When a user creates a Canvas, the client side should initiate a connection to 
the SignalR websocket group, and so on for other joining members. They are deemed the **Leader**. Similarly, when a user creates the party through an invite, they are also initating a connection with the **PartyHub** connection group. 

# Pros
- No orphan parties, if a user creates n parties and doesn't collaborate with any users, they are not left with n orphaned parties.