using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

/// <summary>
/// Goods received against a PO were returned to the supplier (e.g. broken on arrival).
/// Reduces the net received quantity — and therefore the amount owed — and triggers a
/// matching stock reduction for each returned product.
/// </summary>
public record PurchaseOrderReturned(
    PurchaseOrderId PurchaseOrderId,
    IReadOnlyList<ReturnLineData> Lines,
    string Reason,
    string ReturnedBy,
    DateTime OccurredAt) : IDomainEvent;

public record ReturnLineData(Guid ProductId, decimal ReturnedQty);
