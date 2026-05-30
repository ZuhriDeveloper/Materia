using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductCategoryAssigned(
    ProductId ProductId,
    CategoryId CategoryId,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
