using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductDeactivated(
    ProductId ProductId,
    string DeactivatedBy,
    DateTime OccurredAt) : IDomainEvent;
