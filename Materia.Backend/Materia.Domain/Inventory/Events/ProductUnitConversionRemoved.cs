using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductUnitConversionRemoved(
    ProductId ProductId,
    string ToUnit,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
