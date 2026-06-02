using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record PurchaseOrderCancelled(
    PurchaseOrderId PurchaseOrderId,
    string Reason,
    string CancelledBy,
    DateTime OccurredAt) : IDomainEvent;
