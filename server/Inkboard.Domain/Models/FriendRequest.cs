using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkboard.Domain.Models;

[Table("Friend_Requests")]
public class FriendRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequesterId { get; set; }

    public Guid RequesteeId { get; set; }

    public RequestStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Nav prop
    public User Requester { get; set; } = null!;

    public User Requestee { get; set; } = null!;
}

public enum RequestStatus
{
    Pending, // 0
    Accepted,
    Declined,
    Revoked,
}
