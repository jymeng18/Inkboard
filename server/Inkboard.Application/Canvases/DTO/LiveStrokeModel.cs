namespace Inkboard.Application.Canvases.DTO;

// An in-progress stroke frame. Data is the opaque JSON batch (id, tool, colour,
// size, incremental points) the server only relays. UserId is stamped server-side
// so a client can't spoof another user's live stroke.
public readonly record struct LiveStrokeModel(Guid? UserId, string Data);
