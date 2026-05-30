using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductActivated(
    ProductId ProductId,
    string ActivatedBy,
    DateTime OccurredAt) : IDomainEvent;
