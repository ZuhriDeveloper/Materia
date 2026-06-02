using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.CancelPurchaseOrder;

public sealed class CancelPurchaseOrderCommandHandler(IPurchaseOrderRepository repository)
{
    public async Task HandleAsync(CancelPurchaseOrderCommand command, CancellationToken ct = default)
    {
        var po = await repository.GetByIdAsync(PurchaseOrderId.From(command.PurchaseOrderId), ct)
            ?? throw new DomainException($"Purchase order {command.PurchaseOrderId} not found.");

        po.Cancel(command.Reason, command.CancelledBy);
        await repository.SaveAsync(po, ct);
    }
}
