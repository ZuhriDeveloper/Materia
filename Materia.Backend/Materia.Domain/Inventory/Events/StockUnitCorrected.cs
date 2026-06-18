using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record StockUnitCorrected(
    StockId StockId,
    ProductId ProductId,
    string Unit,
    string CorrectedBy,
    DateTime OccurredAt) : IDomainEvent;
