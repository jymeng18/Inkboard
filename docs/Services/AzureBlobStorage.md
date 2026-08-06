# Azure Blob Storage

Canvas snapshots are persisted to a private Azure Blob container. A snapshot is a full PNG
render of the canvas at a point in time. It is saved when a session ends (last user leaves the
canvas) and on a scheduled interval, roughly every 15 minutes, while a canvas is active.

- We do not snapshot every few seconds. It is expensive and unnecessary.
- When a user joins a canvas after work has already happened, we do not force a fresh snapshot.
  Instead we serve the most recent snapshot and then apply every `CanvasOperation` recorded
  after that snapshot's timestamp on top of it. `[latest snapshot] + [ops since SnapshotTakenAt]
  = current canvas state`. This is a catch-up, not a literal replay engine, the newcomer is just
  brought up to the same synced state everyone else is already on.

## Storage model

- Container name is `inkboard-canvases`, created once at startup if it does not exist.
- The container is **private** (`PublicAccessType.None`). We do not want the canvas artwork
  fetchable by anyone who happens to have a URL.
- **One blob per canvas, overwritten each time.** The key is `canvas/{canvasId}.png`. There is no
  timestamped history, the newest snapshot replaces the previous one.
- Format is **PNG**. Lossless and matches the Konva `stage.toBlob()` default. We considered JPEG
  for lower storage cost but chose PNG for quality. This can be revisited later if storage cost
  becomes a concern.

## Serving a private snapshot (SAS tokens)

Because the container is private, clients cannot read the blob directly. On join, the server
generates a short-lived **SAS (Shared Access Signature) URL** for the canvas blob and hands it to
the requesting member. Every member of the canvas can use the SAS URL to download the snapshot
straight from Azure, so the image bytes never flow through our API.

`Canvas.SnapshotURL` still stores the blob's base URL for the record, it is updated on every
snapshot upload. It is not directly usable by clients on its own, the server layers a SAS token on
top of it when serving a join.

## The SnapshotTakenAt reference point

`Canvas` carries a `SnapshotTakenAt` timestamp that marks the point the catch-up query keys off:
on join we fetch `CanvasOperations WHERE Timestamp > SnapshotTakenAt`.

Set `SnapshotTakenAt` **conservatively, to when the owner's client started rendering the stage
(before `toBlob()`), not to upload-completion time.** For a paint canvas, applying a stroke twice
is visually harmless because the pixels are identical, but *missing* a stroke leaves the joiner
permanently out of sync. Erring early means we over-include a few ops rather than risk a gap, and
it also tolerates the fact that operation persistence is asynchronous, an op may not have flushed
to the database yet at the instant the snapshot was taken.

## Join flow, buffer then catch-up

A newcomer must not lose operations that other users draw *while* the newcomer is still loading.
The ordering below guarantees convergence. The frontend shows a loading screen
("Setting up environment for X user, please be patient...") for the duration.

1. Join the CanvasHub group first, and **start buffering** every live operation that arrives. Do
   not apply buffered ops yet.
2. Fetch the snapshot (via SAS URL) and query `CanvasOperations WHERE Timestamp > SnapshotTakenAt`.
3. Apply the snapshot, then apply the historical ops in `(Timestamp, UserId)` order.
4. **Flush the buffer** on top, deduping against the historical set by operation `Id`, then switch
   into live mode.
5. Dismiss the loading screen.

Doing the DB query before joining the group would drop anything drawn in between and the joiner
would silently desync, the loading screen would make a broken join look successful. Join-then-buffer
is what makes the catch-up actually converge.

## PNG and background

PNG supports an alpha channel, so a transparent Konva stage exports cleanly. If we ever switch back
to JPEG, note JPEG has no alpha and fills transparent regions black, the stage would need an opaque
(white) background before export.

## Interface

`IBlobStorageService` lives in `Inkboard.Application/Interfaces` and has no knowledge of Azure, it is
a pure abstraction. The concrete `BlobStorageService` belongs in `Inkboard.Infra/Azure` and uses the
Azure SDK directly. `CanvasService` should be the only caller, the API layer and hubs should never
touch blob storage.

```csharp
public interface IBlobStorageService
{
    Task CreateBlobContainerAsync();
    Task<string> UploadBlobAsync(Guid canvasId, Stream imageData, string contentType);
}
```

`CreateBlobContainerAsync` runs once at startup and creates the private container if it is missing.
`UploadBlobAsync` overwrites `canvas/{canvasId}.png` and returns the blob URL so `CanvasService` can
update `Canvas.SnapshotURL` and `Canvas.SnapshotTakenAt` right after.
