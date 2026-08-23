# Development Roadmap

- [x] Project setup (monorepo, Clean Architecture, Vite + React)
- [x] DB schema — Users, RefreshTokens tables
- [x] JWT auth — register, login, refresh, logout
- [x] Party system — create, invite, kick, block, leave
- [x] SignalR party hub — real-time party notifications
- [x] SignalR canvas sync — real-time drawing broadcast

- [x] Frontend canvas UI — Konva.js drawing layer
- [x] Frontend auth UI — login/register pages
- [x] Frontend party UI — party management
- [x] Persistence — canvas snapshots
- [ ] Polish — reconnection, cursors, undo/redo, offline state

# Codebase Fixes/Improvements
- [x] Change custom ClaimsTypes mapping to standard default NameIdentifier
- [x] Currently PartyHub does not authroize user before allowing data to be pushed -> will need to configure auth for socket connection, as it reads bearer tokens differently then the rest api endpoints 
- [x] Improve docs
- [x] Refactor PartyEndpoint.cs, excessive try catch exceptions
- [x] Delete canvas needs ownership check
- [x] Improve/refactor test suite for future use
- [x] Canvas should display on dashboard for other canvas collaborators too, they can access it again too
- [ ] Search bar for inviting by email
- [x] Globalexceptionhandler
- [ ] Unblock user endpoint + fix bad business logic with blocking users
- [x] Currently when a party dissovles, clients webscokets are not removed from the group
- [x] PartyService and CanvasService are tightly coupled, some methods write into each others states
- [ ] Rate limiting
- [ ] http logging
- [ ] User Account page / User preferences
- [ ] DMs page
- [ ] OAuth for Google / Github
- [ ] Help Docs -> AI Chatbot
- [ ] TOS
- [ ] Dockerize and Deploy
- [x] Error page 404
- [ ] Users not properly displayed as being offline / online
- [ ] Optimize CanvasService.SaveOperationsAsync()