using FluentValidation;

namespace Materia.Application.Validators.Auth;

/// <summary>
/// Shared password rule mirroring the Identity policy configured in Infrastructure
/// (min 8 chars, at least one digit, at least one uppercase letter). Applying it
/// client-side gives a friendly message before the request reaches Identity.
/// </summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.");
}
