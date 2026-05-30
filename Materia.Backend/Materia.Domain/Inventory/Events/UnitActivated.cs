using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record UnitActivated(UnitId UnitId, string ActivatedBy, DateTime OccurredAt) : IDomainEvent;
