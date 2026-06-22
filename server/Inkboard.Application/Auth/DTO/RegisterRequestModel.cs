using System.ComponentModel.DataAnnotations;

namespace Inkboard.Application.Auth.DTO;

public class RegisterRequestModel
{
    [Required(ErrorMessage = "Username is required.")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
    [MaxLength(30, ErrorMessage = "Username must not exceed 30 characters.")]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [MaxLength(40, ErrorMessage = "Email must not exceed 40 characters.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; }
}
