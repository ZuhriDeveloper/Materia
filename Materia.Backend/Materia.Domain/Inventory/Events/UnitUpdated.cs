using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record UnitUpdated(
    UnitId UnitId,
    string Name,
    string? Symbol,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
