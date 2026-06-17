using Application.DTOs.Auth;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validation rules for login: both fields must be present. We do NOT check
/// password rules here — login only confirms the input is well-formed; whether the
/// credentials are correct is decided by the service against the stored hash.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}
