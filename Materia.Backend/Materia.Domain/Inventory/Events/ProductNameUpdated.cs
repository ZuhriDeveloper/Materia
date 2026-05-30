using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductNameUpdated(
    ProductId ProductId,
    string Name,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
