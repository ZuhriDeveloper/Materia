using FluentValidation;

namespace Materia.Application.Commands.Inventory.AdjustStock;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Delta cannot be zero.");
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);
        RuleFor(x => x.AdjustedBy).NotEmpty();
    }
}
