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

        // Optional standing discount chain: at most 6 levels, each in [0, 100).
        RuleFor(x => x.Discounts!)
            .Must(d => d.Count <= 6)
            .WithMessage("A discount chain may have at most 6 levels.")
            .When(x => x.Discounts is not null);
        RuleForEach(x => x.Discounts!)
            .InclusiveBetween(0m, 99.999999m)
            .WithMessage("Each discount must be between 0 and 100 percent.")
            .When(x => x.Discounts is not null);
    }
}
