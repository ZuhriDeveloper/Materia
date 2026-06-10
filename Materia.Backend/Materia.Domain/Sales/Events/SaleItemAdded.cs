using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

/// <summary>
/// Raised each time an item line is added to a sale.
/// <para>
/// Back-compat: <see cref="ListUnitPrice"/>, <see cref="DiscountPerUnit"/>,
/// <see cref="UnitCost"/>, <see cref="AppliedPromotionId"/>, and
/// <see cref="PromotionLabel"/> are trailing optional parameters so that older
/// persisted events (which lack these fields) still deserialise without error.
/// </para>
/// </summary>
public record SaleItemAdded(
    SaleId     SaleId,
    SaleItemId ItemId,
    Guid       ProductId,
    string     ProductName,
    string     UnitName,
    decimal    Quantity,
    decimal    QuantityInBaseUnit,
    /// <summary>Net (as-charged) unit price — the effective price the customer pays.</summary>
    decimal    UnitPrice,
    string     UpdatedBy,
    DateTime   OccurredAt,
    Guid?      VariantId         = null,
    string?    ColorName         = null,
    /// <summary>Gross list price before any line discount. Defaults to <see cref="UnitPrice"/> when null.</summary>
    decimal?   ListUnitPrice     = null,
    /// <summary>Per-unit discount off the list price. Must be 0 ≤ discount ≤ list price.</summary>
    decimal?   DiscountPerUnit   = null,
    /// <summary>Latest supplier purchase price (cost basis) at time of sale. 0 when no price is on record.</summary>
    decimal?   UnitCost          = null,
    Guid?      AppliedPromotionId = null,
    string?    PromotionLabel    = null) : IDomainEvent;
