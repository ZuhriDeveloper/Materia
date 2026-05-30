using FluentValidation;

namespace Materia.Application.Commands.Inventory.AddUnitConversion;

public class AddUnitConversionCommandValidator : AbstractValidator<AddUnitConversionCommand>
{
    public AddUnitConversionCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ToUnit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Factor).GreaterThan(0).WithMessage("Factor must be greater than zero.");
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
