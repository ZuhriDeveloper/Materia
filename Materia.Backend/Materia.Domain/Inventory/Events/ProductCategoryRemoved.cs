using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductCategoryRemoved(
    ProductId ProductId,
    CategoryId CategoryId,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
