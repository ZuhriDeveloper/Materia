using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record UnitDeactivated(UnitId UnitId, string DeactivatedBy, DateTime OccurredAt) : IDomainEvent;
