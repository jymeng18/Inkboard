using System.ComponentModel.DataAnnotations;

namespace Inkboard.Domain;

public class User
{
  public int Id { get; set; }

  [Required]
  [MaxLength(30)]
  public String firstName { get; set; } = string.Empty;


}
