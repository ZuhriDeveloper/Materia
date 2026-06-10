using FluentValidation;

namespace Materia.Application.Commands.Stores.RegisterStore;

public class RegisterStoreCommandValidator : AbstractValidator<RegisterStoreCommand>
{
    public RegisterStoreCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.RegisteredBy).NotEmpty();
    }
}
