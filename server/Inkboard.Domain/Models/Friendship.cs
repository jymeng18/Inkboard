using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Domain.Models;

[Table("Friendships")]
[PrimaryKey(nameof(UserId1), nameof(UserId2))]
public class Friendship
{
    /// <summary>
    /// Constraint before init. any records: UserId1 < UserId2,
    /// otherwise our records can have AB exist, as well as BA exist, 
    /// but thats technically the same exact friends pair
    /// </summary>
    public Guid UserId1 { get; set; }

    public Guid UserId2 { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // nav prop
    public User User1 { get; set; } = null!;

    public User User2 { get; set; } = null!;
}
