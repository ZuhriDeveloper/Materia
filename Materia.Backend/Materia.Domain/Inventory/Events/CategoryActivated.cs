using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record CategoryActivated(
    CategoryId CategoryId,
    string ActivatedBy,
    DateTime OccurredAt) : IDomainEvent;
