# Authentication

JWT-based auth. The decisions here are shaped almost entirely by one threat: **XSS stealing a long-lived credential.** That's what drives where each token lives.

## Token lifetimes

- **Access token — 60 minutes.** A signed JWT carrying the user's id, email, and display name as claims.
- **Refresh token — 7 days.** An opaque token; only its hash is stored server-side.

## The core decision: where each token lives

Two independent choices, each aimed at shrinking what an XSS can do:

- **Access token → in memory only.** It's held in a JS variable, never in a cookie or localStorage. It has to be JS-readable because the client attaches it as an `Authorization` header, so we can't hide it from an XSS entirely — but keeping it out of any persistent store means there's nothing to lift after the fact, and it's gone on reload. It's short-lived, so an exfiltrated one expires fast.
- **Refresh token → httpOnly cookie, set by the server.** This is the long-lived, high-value credential, so it must be untouchable by JavaScript. httpOnly can only come from the server's `Set-Cookie`, so the client never creates or reads it — the browser attaches it automatically, and only to the auth path (the cookie is scoped there, `SameSite=Strict`, and `Secure` outside development).

**Why this split matters:** an XSS on the page can still abuse the *current* session (it runs as the user), but it cannot walk away with the 7-day refresh token to mint new sessions indefinitely. That turns "account takeover" into "one short-lived session token." The thing that actually prevents the XSS in the first place is escaped rendering and no raw-HTML sinks; a Content-Security-Policy is the planned next layer for shrinking the blast radius further.

## Why header auth, and what it means for CSRF

Every protected endpoint authenticates from the **`Authorization` header**, never from a cookie. That makes the API immune to CSRF: a cross-site page can make the browser auto-send cookies, but it cannot set the `Authorization` header, and the server ignores cookies for auth.

The **one exception is the refresh endpoint**, which by necessity reads the refresh cookie (you need it precisely when the access token is gone). That makes it the only CSRF-reachable route — and it's safe anyway, because a cross-site caller can't read the rotated tokens back (CORS locks responses to our own origin) and `SameSite=Strict` blocks the cross-site send outright. So the refresh cookie's `SameSite` assumes the app and API are deployed same-site; a genuinely cross-site deployment would need to revisit that.

## Session flow

- **Register** creates the account but returns no tokens; the client logs in right after to get a session.
- **Login / refresh** return only the access token in the body and set the refresh cookie. The client holds the access token in memory and derives the user's identity by decoding its claims.
- **On page load** the client attempts a silent refresh off the cookie to restore the session before routing decides anything (a signed-in user reloading shouldn't be bounced to login). Concurrent refreshes are deduped into a single in-flight request.
- **On a 401** the client tries one silent refresh and replays the request; if that fails, it clears the in-memory session and redirects to login.
- **Refresh rotates** the token: the old one and any other active tokens for that user are revoked. **Logout** revokes it server-side and clears the cookie.

## SignalR hubs

Browsers can't set custom headers on a WebSocket handshake, so hub connections pass the access token as a query parameter instead of an `Authorization` header. The token still comes from the in-memory session.
