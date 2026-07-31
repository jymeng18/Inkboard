namespace Inkboard.Application.Parties.DTO;

// String rather than Guid so a malformed id is a 400 we word ourselves, matching
// how InviteUserRequest handles ids that arrive from the client.
public record SetPartyCanvasRequest(string CanvasId);
