using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductSalePriceChanged(
    ProductId ProductId,
    decimal   SalePrice,
    string    ChangedBy,
    DateTime  OccurredAt) : IDomainEvent;
