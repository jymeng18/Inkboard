# Azure Blob Storage

When a user joins a Party as a Canvas Collaborator. We send over the latest snapshot that was saved to 
blob storage. A snapshot is made and saved when a session ends (last user leaves the canvas), and on a scheduled interval
like every 15 minutes on an active Canvas. 

- We do not save a snapshot every few seconds of the Canvas as it is expensive and unecessary. 
- If user joins in late and the work on the Canvas has happened way after a snapshot was made --> we set up a workflow
where [Snapshot from 11 mins ago] + [CanvasOperations since then] = Full current canvas state. We are essentially running a 
replay feature under the hood.

# Minor Configuration Notes
Storage account on Azure is set to allow anonymous access on blob storage snapshots. This means we do not need a direct endpoint on the server asking for a Canvas snapshot, client side can directly call it and receive it. 