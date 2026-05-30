using FluentValidation;
using Materia.Application.Contracts.Inventory;

namespace Materia.Application.Commands.Inventory.CreateUnit;

public class CreateUnitCommandValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitCommandValidator(IUnitQueryRepository unitQueryRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(async (name, ct) => !await unitQueryRepository.ExistsByNameAsync(name, null, ct))
            .WithMessage("Satuan sudah terdaftar.");

        RuleFor(x => x.Symbol).MaximumLength(20).When(x => x.Symbol is not null);
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}
