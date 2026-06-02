using Materia.Domain.Common;
using Materia.Domain.Purchasing;

namespace Materia.Domain.Inventory.Events;

public record StockReconciledFromPurchase(
    StockId StockId,
    ProductId ProductId,
    decimal ReceivedQty,
    decimal NewQuantity,
    PurchaseOrderId PurchaseOrderId,
    decimal UnitCost,
    string Unit,
    string ReconciledBy,
    DateTime OccurredAt) : IDomainEvent;
