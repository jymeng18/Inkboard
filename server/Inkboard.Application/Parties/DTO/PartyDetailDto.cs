namespace Inkboard.Application.Parties.DTO;

public record PartyDetailDto(
    Guid Id,
    Guid LeaderId,
    Guid? CanvasId,
    List<PartyMemberDto> Members
);

public record PartyMemberDto(
    Guid UserId,
    string Role,
    DateTime JoinedAt
);
