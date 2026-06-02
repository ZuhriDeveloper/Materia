using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.ConfirmPurchaseOrder;

public sealed class ConfirmPurchaseOrderCommandHandler(IPurchaseOrderRepository repository)
{
    public async Task HandleAsync(ConfirmPurchaseOrderCommand command, CancellationToken ct = default)
    {
        var po = await repository.GetByIdAsync(PurchaseOrderId.From(command.PurchaseOrderId), ct)
            ?? throw new DomainException($"Purchase order {command.PurchaseOrderId} not found.");

        po.Confirm(command.ConfirmedBy);
        await repository.SaveAsync(po, ct);
    }
}
