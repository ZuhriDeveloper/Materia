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
