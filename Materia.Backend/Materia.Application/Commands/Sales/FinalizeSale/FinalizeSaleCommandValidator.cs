using FluentValidation;

namespace Materia.Application.Commands.Sales.FinalizeSale;

public sealed class FinalizeSaleCommandValidator : AbstractValidator<FinalizeSaleCommand>
{
    public FinalizeSaleCommandValidator()
    {
        RuleFor(x => x.ServedBy).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerName).MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Penjualan harus memiliki minimal satu item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.UnitName).NotEmpty().MaximumLength(50);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
