using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record PurchaseOrderConfirmed(
    PurchaseOrderId PurchaseOrderId,
    string ConfirmedBy,
    DateTime OccurredAt) : IDomainEvent;
