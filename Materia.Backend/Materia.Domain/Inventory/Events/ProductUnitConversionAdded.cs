using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductUnitConversionAdded(
    ProductId ProductId,
    string FromUnit,
    string ToUnit,
    decimal Factor,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
