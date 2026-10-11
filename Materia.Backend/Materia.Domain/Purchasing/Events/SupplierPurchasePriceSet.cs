using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Domain.Purchasing.Events;

public record SupplierPurchasePriceSet(
    SupplierId SupplierId,
    ProductId ProductId,
    decimal Amount,
    string Currency,
    string Unit,
    DateTime EffectiveFrom,
    string UpdatedBy,
    DateTime OccurredAt,
    // Standing chained trade discount on the list price. Optional/last for back-compat:
    // events persisted before this field deserialize to null and are treated as no discount.
    IReadOnlyList<decimal>? Discounts = null) : IDomainEvent;
