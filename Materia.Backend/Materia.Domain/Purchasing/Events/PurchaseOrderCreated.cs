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
    string? PaymentTermUnit = null,
    // When true, receiving goods writes the line's list price back to the supplier
    // catalog. Appended optional — legacy events deserialise as false.
    bool UpdateCatalogOnReceipt = false) : IDomainEvent;

public record PurchaseOrderLineData(
    Guid ProductId,
    decimal OrderedQty,
    decimal UnitCost,
    string Unit,
    // Buy price before the per-line chained discount, and the chain itself (% per level).
    // UnitCost stays the NET. Appended after the original shape — legacy events:
    // ListUnitCost ?? UnitCost, Discounts ?? [].
    decimal? ListUnitCost = null,
    IReadOnlyList<decimal>? Discounts = null);
