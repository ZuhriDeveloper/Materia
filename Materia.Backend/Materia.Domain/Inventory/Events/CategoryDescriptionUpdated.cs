using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record CategoryDescriptionUpdated(
    CategoryId CategoryId,
    string? Description,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
