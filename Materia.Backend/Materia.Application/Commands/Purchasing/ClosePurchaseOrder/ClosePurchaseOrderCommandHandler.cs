using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.ClosePurchaseOrder;

/// <summary>
/// Short-closes a partially-received PO when the supplier won't ship the remainder.
/// Received goods stay in stock (no stock change); only the PO lifecycle is finalized.
/// </summary>
public sealed class ClosePurchaseOrderCommandHandler(IPurchaseOrderRepository repository)
{
    public async Task HandleAsync(ClosePurchaseOrderCommand command, CancellationToken ct = default)
    {
        var po = await repository.GetByIdAsync(PurchaseOrderId.From(command.PurchaseOrderId), ct)
            ?? throw new DomainException($"Purchase order {command.PurchaseOrderId} not found.");

        po.Close(command.Reason, command.ClosedBy);
        await repository.SaveAsync(po, ct);
    }
}
