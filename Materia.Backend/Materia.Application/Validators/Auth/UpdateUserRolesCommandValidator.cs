using FluentValidation;
using Materia.Application.Commands.Auth;
using Materia.Application.Contracts.Auth;

namespace Materia.Application.Validators.Auth;

public class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
{
    public UpdateUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role must be selected.");

        // Only the store roles may be granted here — never SuperAdmin (no privilege escalation).
        RuleForEach(x => x.Roles)
            .Must(role => RoleNames.AssignableStoreRoles.Contains(role))
            .WithMessage("One or more selected roles cannot be assigned.");
    }
}
