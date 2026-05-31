using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleConfirmed(
    SaleId   SaleId,
    decimal  GrandTotal,
    string   ConfirmedBy,
    DateTime OccurredAt) : IDomainEvent;
