using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkboard.Domain.Models;


[Table("Party_Invites")]
public class PartyInvite
{
  [Key]
  public Guid Id { get; set; } = Guid.NewGuid();

  public Guid PartyId { get; set; }

  public Guid InvitedByUserId { get; set; }

  public Guid InvitedUserId { get; set; }

  public InviteStatus InviteStatus { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public DateTime ExpiresAt { get; set; }


  // TODO: Two FK's pointing at User table, --> EF Core issue,
  // need to use fluent api to explcitly map it out

  // FK: InvitedByUserId -> Users.Id 
  public User InvitedBy { get; set; } = null!;

  // FK: InvitedUserId -> Users.Id
  public User InvitedUser { get; set; } = null!;

  // FK: PartyId -> Party.Id
  public Party Party { get; set; } = null!;
}

public enum InviteStatus
{
  Pending, 
  Accepted,
  Declined
}