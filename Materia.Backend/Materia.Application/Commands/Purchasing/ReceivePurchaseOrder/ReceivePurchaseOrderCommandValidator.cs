using FluentValidation;

namespace Materia.Application.Commands.Purchasing.ReceivePurchaseOrder;

public sealed class ReceivePurchaseOrderCommandValidator : AbstractValidator<ReceivePurchaseOrderCommand>
{
    public ReceivePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.ReceivedBy).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.ReceivedQty).GreaterThan(0);
            line.RuleFor(l => l.VariantId!.Value)
                .NotEmpty()
                .When(l => l.VariantId.HasValue)
                .WithMessage("VariantId must not be an empty GUID when provided.");
        });
    }
}
