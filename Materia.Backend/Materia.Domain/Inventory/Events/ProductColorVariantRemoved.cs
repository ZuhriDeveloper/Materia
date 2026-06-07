using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductColorVariantRemoved(
    ProductId ProductId,
    VariantId VariantId,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
