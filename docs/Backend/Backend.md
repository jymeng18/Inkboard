# Backend Architecture Guide (Clean Architecture)

The backend follows **Clean Architecture** to strictly separate concerns.

Dependencies must always point **inwards** toward the Domain layer:
`API → Infra → Application → Domain`

---

## Layers Breakdown

### 1. Inkboard.Domain
Contains core business models and repository interfaces. **No external dependencies**

---

### 2. Inkboard.Application
Defines the **Use Cases** of the system. Orchestrates business logic but fully abstracts how data is stored or where requests come from.

---

### 3. Inkboard.Infra
Implements interfaces from Application and Domain layers. Knows about external tools: databases, JWT, Azure, Redis, etc.

---

### 4. Inkboard.API
The entry point. Receives HTTP/WebSocket requests, delegates to Application layer, returns responses. **Strictly no business logic.**
