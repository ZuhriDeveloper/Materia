using FluentValidation;
using Materia.Application.Contracts.Inventory;

namespace Materia.Application.Commands.Inventory.UpdateUnit;

public class UpdateUnitCommandValidator : AbstractValidator<UpdateUnitCommand>
{
    public UpdateUnitCommandValidator(IUnitQueryRepository unitQueryRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(async (cmd, name, ct) =>
                !await unitQueryRepository.ExistsByNameAsync(name, cmd.UnitId, ct))
            .WithMessage("Satuan sudah terdaftar.");

        RuleFor(x => x.Symbol).MaximumLength(20).When(x => x.Symbol is not null);
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
