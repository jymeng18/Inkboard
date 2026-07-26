using System.ComponentModel.DataAnnotations;

namespace Inkboard.Application.Auth.DTO;

public class RegisterRequestModel
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}
