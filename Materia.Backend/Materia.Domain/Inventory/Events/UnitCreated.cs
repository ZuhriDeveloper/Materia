using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record UnitCreated(
    UnitId UnitId,
    string Name,
    string? Symbol,
    string CreatedBy,
    DateTime OccurredAt) : IDomainEvent;
