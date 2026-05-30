using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record CategoryCreated(
    CategoryId CategoryId,
    string Name,
    string? Description,
    string CreatedBy,
    DateTime OccurredAt) : IDomainEvent;
