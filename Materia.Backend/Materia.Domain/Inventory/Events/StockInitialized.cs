using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record StockInitialized(
    StockId StockId,
    ProductId ProductId,
    decimal Quantity,
    string Unit,
    string CreatedBy,
    DateTime OccurredAt,
    Guid? VariantId = null) : IDomainEvent;
