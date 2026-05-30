using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductDescriptionUpdated(
    ProductId ProductId,
    string? Description,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
