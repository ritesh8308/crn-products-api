using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validation rules for registration. Username is bounded to match the column
/// (nvarchar(100)); the password has a minimum length so we don't hash trivially
/// weak secrets. Kept intentionally simple — no complexity regex to over-engineer.
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username must not exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}
