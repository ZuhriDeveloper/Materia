using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleItemRemoved(
    SaleId     SaleId,
    SaleItemId ItemId,
    string     UpdatedBy,
    DateTime   OccurredAt) : IDomainEvent;
