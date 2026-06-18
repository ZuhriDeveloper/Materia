using FluentValidation;

namespace Materia.Application.Commands.Inventory.ChangeProductBaseUnit;

public class ChangeProductBaseUnitCommandValidator : AbstractValidator<ChangeProductBaseUnitCommand>
{
    public ChangeProductBaseUnitCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.BaseUnit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ChangedBy).NotEmpty();
    }
}
