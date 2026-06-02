using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleFinalized(
    SaleId   SaleId,
    decimal  GrandTotal,
    string   ServedBy,
    bool     IsDeliveryRequired,
    DateTime OccurredAt) : IDomainEvent;
