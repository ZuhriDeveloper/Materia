using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductColorVariantActivated(
    ProductId ProductId,
    VariantId VariantId,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
