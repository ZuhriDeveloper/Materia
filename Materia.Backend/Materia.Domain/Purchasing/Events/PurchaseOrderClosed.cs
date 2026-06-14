using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

/// <summary>
/// A partially-received PO was finalized short — the supplier will not ship the remaining
/// ordered items (e.g. out of stock). What was received stays in stock and is owed/costed;
/// the undelivered remainder is treated as done.
/// </summary>
public record PurchaseOrderClosed(
    PurchaseOrderId PurchaseOrderId,
    string Reason,
    string ClosedBy,
    DateTime OccurredAt) : IDomainEvent;
