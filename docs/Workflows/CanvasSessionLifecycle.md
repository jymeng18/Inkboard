# Canvas Session Lifecycle

This is the planned design for real time drawing sync and canvas persistence. None of this is implemented yet, `CanvasHub` currently only manages the SignalR connection group. Check `TODO.md` for current status.

## Starting a session

1. Party leader creates a canvas via `POST /api/canvas`.
2. `CanvasService` creates a `Canvas` record with `OwnerId` set to the leader and `SnapshotURL` null.
3. `Party.CanvasId` is set once the first invite goes out.
4. Party members connect to the `CanvasHub` group for this canvas.

## During a session

* Members draw, operations broadcast via `CanvasHub` and persist asynchronously so persistence never blocks the broadcast.
* A frontend interval timer periodically renders the stage to a blob and uploads it as a snapshot, as a safety net against unexpected termination.
* Only the canvas owner's client sends periodic snapshots.

## Ending a session, owner clicks end session

1. Frontend renders the stage to a blob.
2. Frontend uploads the blob as the final snapshot.
3. `CanvasService` verifies the requester is the owner, then notifies all group members to disconnect.
4. `Party.CanvasId` is set back to null. The party stays alive.

## Ending a session, leadership transfer mid session

1. The canvas owner transfers party leadership through the normal party flow.
2. `PartyService` detects an active canvas on the party and calls `CanvasService.ForceEndSessionAsync`.
3. All group members are notified to disconnect. No final snapshot, the last periodic one is the final state.
4. `Party.CanvasId` is set back to null. Party leadership transfers as normal.

## Ending a session, unexpected owner disconnect

1. `CanvasHub.OnDisconnectedAsync` fires for the disconnecting user.
2. If that user is the canvas owner, the hub calls `CanvasService.ForceEndSessionAsync`.
3. Remaining members are notified to disconnect. No final snapshot.
4. `Party.CanvasId` is set back to null.

## Drawing sync

1. A stroke renders locally right away, optimistic rendering, no wait for the server.
2. The stroke is sent to `CanvasHub` as a lightweight operation object.
3. The hub broadcasts it to every other client in the same canvas group.
4. Other clients render it on receipt.
5. The operation persists to the database asynchronously.

Cursor sync is a separate, never persisted path. See `../Services/CanvasCursorTracking.md`.

## Snapshot upload

1. Triggered by the periodic timer or by ending the session.
2. Frontend renders the Konva stage to a PNG blob.
3. Frontend uploads the blob as multipart form data.
4. `CanvasService` verifies the requester is the owner.
5. `CanvasService` calls `IBlobStorageService.UploadSnapshotAsync`, see `../Services/AzureBlobStorage.md`.
6. `Canvas.SnapshotURL` is updated with the returned blob URL.
7. A user joining an existing session receives the latest `SnapshotURL` immediately, and replays any `CanvasOperations` recorded since that snapshot to reach the current state.

## Conflict resolution

When two users draw over the same area at the same time, the rule is last write wins with deterministic ordering.

* Every operation has a timestamp and a userId.
* Operations apply in timestamp order.
* A tie on timestamp is broken by userId.
* Acceptable for a paint style canvas, where pixel level conflicts are visually tolerable.

## Latency minimization

Design decisions that exist specifically to keep the delay between one user's action and another user seeing it as small as possible.

* WebSockets via SignalR for every real time event. REST is never used for drawing or cursor data.
* Optimistic local rendering before the server confirms anything.
* Only the delta goes over the wire, the new stroke or cursor position, never the full canvas state.
* Persistence is asynchronous and never blocks the broadcast.
* Cursor events are throttled client side and never acknowledged by the server.
* Periodic snapshot uploads are fire and forget from the frontend's perspective. Only the end of session snapshot waits for confirmation before the session actually ends.
