using FluentValidation;

namespace Materia.Application.Commands.Purchasing.CancelPurchaseOrder;

public sealed class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CancelledBy).NotEmpty().MaximumLength(100);
    }
}
