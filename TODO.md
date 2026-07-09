# Development Roadmap

- [x] Project setup (monorepo, Clean Architecture, Vite + React)
- [x] DB schema — Users, RefreshTokens tables
- [x] JWT auth — register, login, refresh, logout
- [x] Party system — create, invite, kick, block, leave
- [x] SignalR party hub — real-time party notifications
- [] SignalR canvas sync — real-time drawing broadcast

- [ ] Frontend canvas UI — Konva.js drawing layer
- [ ] Frontend auth UI — login/register pages
- [ ] Frontend party UI — party management
- [ ] Persistence — canvas snapshots
- [ ] Polish — reconnection, cursors, undo/redo, offline state

# Codebase Fixes/Improvements
- [ ] Change custom ClaimsTypes mapping to standard default NameIdentifier
- [ ] Currently PartyHub does not authroize user before allowing data to be pushed -> will need to configure auth for socket connection, as it reads bearer tokens differently then the rest api endpoints 
- [ ] Refactor PartyEndpoint.cs, excessive try catch exceptions