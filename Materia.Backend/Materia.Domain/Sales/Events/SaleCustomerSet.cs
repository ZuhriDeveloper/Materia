using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleCustomerSet(
    SaleId   SaleId,
    Guid?    CustomerId,
    string   CustomerName,
    string   UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
