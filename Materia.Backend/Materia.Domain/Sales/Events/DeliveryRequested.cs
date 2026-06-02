using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record DeliveryRequested(
    SaleId   SaleId,
    string   RequestedBy,
    DateTime OccurredAt) : IDomainEvent;
