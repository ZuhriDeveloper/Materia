using FluentValidation;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    private static readonly string[] AllowedUnits =
        Enum.GetNames<PaymentTermUnit>();

    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Qty).GreaterThan(0);

            // Optional chained discount: at most 6 levels, each in [0, 100).
            line.RuleFor(l => l.Discounts!)
                .Must(d => d.Count <= 6)
                .WithMessage("A discount chain may have at most 6 levels.")
                .When(l => l.Discounts is not null);
            line.RuleForEach(l => l.Discounts!)
                .InclusiveBetween(0m, 99.999999m)
                .WithMessage("Each discount must be between 0 and 100 percent.")
                .When(l => l.Discounts is not null);

            // Optional list-price override: must be positive when provided.
            line.RuleFor(l => l.ListUnitCost!.Value)
                .GreaterThan(0)
                .WithMessage("Buy price must be greater than 0.")
                .When(l => l.ListUnitCost is not null);
        });

        // Payment tenor is optional (null = cash); when supplied it must be valid and complete.
        When(x => x.PaymentTermValue is not null || x.PaymentTermUnit is not null, () =>
        {
            RuleFor(x => x.PaymentTermValue)
                .NotNull().GreaterThan(0)
                .WithMessage("Payment term period must be a positive number.");
            RuleFor(x => x.PaymentTermUnit)
                .NotEmpty()
                .Must(u => u is not null && AllowedUnits.Contains(u, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Payment term unit must be one of: {string.Join(", ", AllowedUnits)}.");
        });
    }
}
