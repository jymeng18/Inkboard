# Canvas Snapshots (Azure Blob Storage)

How we persist a canvas as an image and get it back to people joining a session. This records the decisions and the problems that shaped them, not the code.

## What a snapshot is, and when we take one

A snapshot is a full PNG render of the canvas at a point in time. We take one on session end (last user leaves) and on a slow interval (~15 min) while a canvas is active.

- **Decision: don't snapshot every few seconds.** It is expensive in storage and unnecessary. The append-only operation log already carries the fine-grained history.
- **Decision: PNG, not JPEG.** Lossless and matches the Konva stage export. We accepted the larger size for quality; revisit if storage cost ever bites. (If we ever go back to JPEG, note JPEG has no alpha and fills transparent regions black, so the stage would need an opaque background before export.)

## Storage shape

- Container `inkboard-canvases`, **private**. We do not want anyone with a URL pulling other people's artwork.
- **One blob per canvas, overwritten each time** (`canvas/{canvasId}.png`). No timestamped history. The newest snapshot replaces the previous one; the operation log is where history lives.

## Problem: a private container can't be read by the browser

The browser has no way to authenticate to a private container. Two ways out:

1. **SAS token** — the server mints a short-lived signed URL and the browser reads the blob directly from Azure.
2. **Server-proxied read** — the browser asks our API, the server reads the blob and returns the bytes.

### Decision: server-proxied read

We already authenticate the server to Azure with Entra ID (managed identity + RBAC). The key realization: **RBAC authorizes the identity making the call.** The server is a principal Azure recognizes; our end users are not (they hold our app's JWT, not an Azure identity, and we would never grant every user a role on the storage account). So the SAS route was only ever needed to let a credential-less browser read directly.

Since the server *does* hold the credential, we let it do the read and hand the bytes back. This:

- **drops all SAS machinery** — no user-delegation keys, no token expiry juggling, no per-blob signing;
- **reuses the RBAC we already wired**;
- **gives per-request authorization** — the server checks the caller belongs to the canvas before streaming, instead of handing out a bearer URL valid for anyone who holds it.

The tradeoff is that snapshot bytes flow through our API instead of straight from Azure. At this scale (reads happen only on join, ≤5 users per canvas) that bandwidth is negligible, so the simpler, more controllable option wins. SAS would only earn its complexity if we needed to offload heavy read bandwidth off the API later.

The stored blob URL is kept on the canvas record so the server can locate the blob; it is never handed to the client as a usable URL.

### Two read paths, split by authorization

The read splits into two cases so each has one clear authorization rule instead of a branching one:

- **Live-session read (a member joining):** allowed when the caller's active party is the one bound to this canvas.
- **Owner re-view (from the dashboard, no active session):** allowed when the caller owns the canvas.

Keeping these separate avoids an IDOR we hit early: an authorization check that only asked "is the caller in *some* active party" let any party member pull *any* canvas by id. The fix was to tie the check to *this* canvas — either the caller's active party is bound to it, or the caller owns it.

## Catch-up on join, not a replay engine

A snapshot alone is stale the moment more drawing happens. When someone joins mid-session we serve the latest snapshot, then apply every operation recorded after the snapshot's timestamp on top of it. It is a catch-up to the shared state everyone else is already on, not a general replay system.

### The reference timestamp must be conservative

The canvas record carries the moment the snapshot was taken, and the catch-up applies operations recorded after it. **We set that moment early — to when the client started rendering the stage, not when the upload finished.** For a paint canvas, applying a stroke twice is visually harmless (identical pixels), but *missing* one leaves the joiner permanently out of sync. Erring early over-includes a few operations rather than risking a gap, and it also tolerates operation persistence being asynchronous (an operation may not have flushed to the database yet when the snapshot was taken).

### Problem: operations drawn *while* joining would be lost

The naive order — query the operation log, apply it, then subscribe to live updates — silently drops anything drawn during the first two steps, and the loading screen makes the broken join look successful.

**Solution: join first, buffer, then catch up.** Subscribe to the live channel and buffer incoming operations before doing anything else; then fetch the snapshot and the operations since its timestamp; apply the snapshot and that history; then flush the buffer on top (deduped by operation id) and go live. A loading screen ("Setting up environment…") covers the interval. Join-then-buffer is what makes the catch-up actually converge.

## Problem: the uploaded payload is untrusted

The snapshot arrives as raw bytes from a client, so we cannot assume it is really our canvas render. We can't prove authenticity (a user can legitimately draw anything), so the goal is resource safety and content safety, layered cheap-to-expensive:

- **Size bounds** (reject too-small and too-large) to stop absurd payloads early.
- **PNG signature check** on the leading bytes, so a renamed non-image is caught without trusting the declared content type.
- **Dimension check from the header before decoding**, to stop a decompression bomb (a few-KB file that expands to a gigapixel bitmap).
- **Full decode as the final gate** — the payload must actually decode as an image, catching truncated or polyglot files that pass the cheap checks. This lives behind an abstraction so the imaging library stays in the infrastructure layer.

Uploads go *through the server* (not direct browser-to-blob) specifically so this validation gates what lands in storage; a direct-upload path would bypass all of it.
