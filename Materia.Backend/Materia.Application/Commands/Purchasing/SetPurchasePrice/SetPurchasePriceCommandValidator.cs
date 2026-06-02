using FluentValidation;

namespace Materia.Application.Commands.Purchasing.SetPurchasePrice;

public sealed class SetPurchasePriceCommandValidator : AbstractValidator<SetPurchasePriceCommand>
{
    public SetPurchasePriceCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty().MaximumLength(100);
    }
}
