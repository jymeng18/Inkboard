using FluentValidation;
using Inkboard.Application.Auth.DTO;

namespace Inkboard.Application.Auth.Handlers;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestModel>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters.")
            .MaximumLength(30)
            .WithMessage("Username must not exceed 30 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email format.")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters.")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters.");

    }
}
