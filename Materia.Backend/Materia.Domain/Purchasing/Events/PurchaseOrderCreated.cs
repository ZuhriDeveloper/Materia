using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record PurchaseOrderCreated(
    PurchaseOrderId PurchaseOrderId,
    SupplierId SupplierId,
    IReadOnlyList<PurchaseOrderLineData> Lines,
    string CreatedBy,
    DateTime OccurredAt,
    // Payment tenor (tempo). Null = cash / no tempo. Appended after the original
    // shape — legacy stored events deserialise these as null.
    int? PaymentTermValue = null,
    string? PaymentTermUnit = null) : IDomainEvent;

public record PurchaseOrderLineData(
    Guid ProductId,
    decimal OrderedQty,
    decimal UnitCost,
    string Unit);
