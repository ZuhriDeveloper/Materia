using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleTypeSetToPickup(
    SaleId   SaleId,
    string   UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
