using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record CategoryNameUpdated(
    CategoryId CategoryId,
    string Name,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
