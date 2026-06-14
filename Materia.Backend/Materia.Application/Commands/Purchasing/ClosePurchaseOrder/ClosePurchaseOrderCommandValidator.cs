using FluentValidation;

namespace Materia.Application.Commands.Purchasing.ClosePurchaseOrder;

public sealed class ClosePurchaseOrderCommandValidator : AbstractValidator<ClosePurchaseOrderCommand>
{
    public ClosePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ClosedBy).NotEmpty().MaximumLength(100);
    }
}
