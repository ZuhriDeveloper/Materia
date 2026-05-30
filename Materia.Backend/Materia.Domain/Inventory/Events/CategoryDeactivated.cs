using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record CategoryDeactivated(
    CategoryId CategoryId,
    string DeactivatedBy,
    DateTime OccurredAt) : IDomainEvent;
