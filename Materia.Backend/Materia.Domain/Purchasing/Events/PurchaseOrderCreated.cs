using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record PurchaseOrderCreated(
    PurchaseOrderId PurchaseOrderId,
    SupplierId SupplierId,
    IReadOnlyList<PurchaseOrderLineData> Lines,
    string CreatedBy,
    DateTime OccurredAt) : IDomainEvent;

public record PurchaseOrderLineData(
    Guid ProductId,
    decimal OrderedQty,
    decimal UnitCost,
    string Unit);
