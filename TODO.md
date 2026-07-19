# Development Roadmap

- [x] Project setup (monorepo, Clean Architecture, Vite + React)
- [x] DB schema — Users, RefreshTokens tables
- [x] JWT auth — register, login, refresh, logout
- [x] Party system — create, invite, kick, block, leave
- [x] SignalR party hub — real-time party notifications
- [ ] SignalR canvas sync — real-time drawing broadcast

- [ ] Frontend canvas UI — Konva.js drawing layer
- [ ] Frontend auth UI — login/register pages
- [ ] Frontend party UI — party management
- [ ] Persistence — canvas snapshots
- [ ] Polish — reconnection, cursors, undo/redo, offline state

# Codebase Fixes/Improvements
- [x] Change custom ClaimsTypes mapping to standard default NameIdentifier
- [x] Currently PartyHub does not authroize user before allowing data to be pushed -> will need to configure auth for socket connection, as it reads bearer tokens differently then the rest api endpoints 
- [x] Improve docs
- [ ] Refactor PartyEndpoint.cs, excessive try catch exceptions
- [ ] Improve/refactor test suite for future use
- [ ] Dissolve parties if no user action for more then 15 minutes(IMPORTANT)
- [ ] Canvas should display on dashboard for other canvas collaborators too, they can access it again too