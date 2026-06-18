using FluentValidation;

namespace Materia.Application.Commands.Purchasing.RecordPurchaseReturn;

public sealed class RecordPurchaseReturnCommandValidator : AbstractValidator<RecordPurchaseReturnCommand>
{
    public RecordPurchaseReturnCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.ReturnedBy).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one return line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.ReturnedQty).GreaterThan(0);
            line.RuleFor(l => l.VariantId!.Value)
                .NotEmpty()
                .When(l => l.VariantId.HasValue)
                .WithMessage("VariantId must not be an empty GUID when provided.");
        });
    }
}
