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
    DateTime OccurredAt) : IDomainEvent;
