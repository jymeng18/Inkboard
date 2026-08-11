using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Domain.Models;

[PrimaryKey(nameof(PartyId), nameof(UserId))]
[Table("Party_Members")]
public class PartyMember
{
    public Guid PartyId { get; set; }

    public Guid UserId { get; set; }

    public UserRole Role { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // nav props
    public Party Party { get; set; }

    // FK(UserID) --> User.id
    public User User { get; set; }
}

public enum UserRole
{
    Leader,
    Member,
}
