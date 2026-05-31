using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleCancelled(
    SaleId   SaleId,
    string   Reason,
    string   CancelledBy,
    DateTime OccurredAt) : IDomainEvent;
