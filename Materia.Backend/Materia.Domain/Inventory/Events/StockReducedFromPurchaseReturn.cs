using Materia.Domain.Common;
using Materia.Domain.Purchasing;

namespace Materia.Domain.Inventory.Events;

/// <summary>
/// Stock lowered because goods received against a purchase order were returned to the
/// supplier. The mirror of <see cref="StockReconciledFromPurchase"/>.
/// </summary>
public record StockReducedFromPurchaseReturn(
    StockId StockId,
    ProductId ProductId,
    decimal ReturnedQty,
    decimal NewQuantity,
    PurchaseOrderId PurchaseOrderId,
    decimal UnitCost,
    string Unit,
    string ReducedBy,
    DateTime OccurredAt) : IDomainEvent;
