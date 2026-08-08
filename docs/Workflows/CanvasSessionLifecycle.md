# Canvas Session Lifecycle

The decisions behind how a collaborative drawing session starts, runs, and ends. Snapshot storage and join catch-up are covered in `../Services/AzureBlobStorage.md`; this is the session shape around them.

## Ownership is fixed at creation

A canvas has one owner, set when it's created, and it never transfers. This is the anchor for every session rule below: only the owner ends a session or uploads the authoritative snapshot. It keeps authorization unambiguous — there is never a question of "who is in charge of this canvas."

## During a session

- Drawing is broadcast to the other clients and persisted asynchronously, so persistence never blocks the broadcast (see latency notes below).
- Only the owner's client uploads periodic snapshots (~15 min), as a safety net against unexpected termination. Non-owners never upload.

## Ending a session

Three ways a session ends, and the deliberate difference between them is **whether a final snapshot is taken**:

- **Owner ends it deliberately** → a final snapshot is captured first, *then* the session tears down. This is why upload and end are two separate steps rather than one: the end action never carries image bytes, and the snapshot path stays the single place that validates and stores an image.
- **Leadership transfer mid-session** → the session is force-ended with **no** final snapshot; the last periodic one stands. Taking a snapshot here would mean trusting a departing owner's client mid-handover, for little gain.
- **Unexpected owner disconnect** (crash, closed tab) → same as transfer: force-end, no final snapshot, last periodic one is the final state.

In every case the party survives; only the canvas link is severed. A party with no active canvas is a valid resting state.

### Who owns severing the party↔canvas link

An open question we flagged: ending a session touches two aggregates — the canvas (notify members, tear down) and the party (null out its canvas link). The cleaner split is for each service to write only its own aggregate: the canvas side handles teardown, the party side owns clearing its own link. This keeps session-ending from turning the party service into a god object. (Recorded as the intended direction; see the code for current state.)

## Drawing sync

A stroke renders locally immediately (optimistic — no wait for the server), is sent as a lightweight operation, broadcast to the other clients, and persisted asynchronously. Cursor sync is a separate, never-persisted path (`../Services/CanvasCursorTracking.md`).

## Conflict resolution

Two people drawing over the same area at once resolve as **last-write-wins with deterministic ordering**: operations apply in timestamp order, ties broken by user id. This is acceptable precisely because it's a paint-style canvas where pixel-level conflicts are visually tolerable — we chose the cheap deterministic rule over real OT/CRDT machinery that the medium doesn't justify.

## Latency minimization

The decisions that exist specifically to keep the delay between one person's action and another seeing it small:

- WebSockets for every real-time event; REST is never on the drawing or cursor path.
- Optimistic local rendering before the server confirms anything.
- Only the delta goes over the wire (the new stroke or cursor position), never the full canvas.
- Persistence is asynchronous and never blocks a broadcast.
- Cursor events are throttled client-side and never acknowledged.
- Periodic snapshot uploads are fire-and-forget; only the end-of-session snapshot waits for confirmation before the session actually ends.
