using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductColorVariantAdded(
    ProductId ProductId,
    VariantId VariantId,
    string ColorName,
    string? ColorCode,
    string? Barcode,
    decimal? PriceOverride,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
