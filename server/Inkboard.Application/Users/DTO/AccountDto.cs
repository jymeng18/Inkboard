namespace Inkboard.Application.Users.DTO;

public record AccountDto(Guid Id, string UserName, string Email, DateTimeOffset CreatedAt);
