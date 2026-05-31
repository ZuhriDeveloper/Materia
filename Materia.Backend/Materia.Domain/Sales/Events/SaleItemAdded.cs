using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleItemAdded(
    SaleId     SaleId,
    SaleItemId ItemId,
    Guid       ProductId,
    string     ProductName,
    string     UnitName,
    decimal    Quantity,
    decimal    QuantityInBaseUnit,
    decimal    UnitPrice,
    string     UpdatedBy,
    DateTime   OccurredAt) : IDomainEvent;
