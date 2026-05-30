using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record StockAdjusted(
    StockId StockId,
    ProductId ProductId,
    decimal Delta,
    decimal NewQuantity,
    string? Reason,
    string AdjustedBy,
    DateTime OccurredAt) : IDomainEvent;
