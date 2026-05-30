using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductCreated(
    ProductId ProductId,
    string Name,
    string? Description,
    string BaseUnit,
    string CreatedBy,
    DateTime OccurredAt) : IDomainEvent;
