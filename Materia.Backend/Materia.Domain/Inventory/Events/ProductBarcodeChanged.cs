using Materia.Domain.Common;

namespace Materia.Domain.Inventory.Events;

public record ProductBarcodeChanged(
    ProductId ProductId,
    string?   Barcode,
    string    ChangedBy,
    DateTime  OccurredAt) : IDomainEvent;
