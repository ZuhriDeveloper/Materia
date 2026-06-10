using FluentValidation;

namespace Materia.Application.Commands.Stores.RenameStore;

public class RenameStoreCommandValidator : AbstractValidator<RenameStoreCommand>
{
    public RenameStoreCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RenamedBy).NotEmpty();
    }
}
