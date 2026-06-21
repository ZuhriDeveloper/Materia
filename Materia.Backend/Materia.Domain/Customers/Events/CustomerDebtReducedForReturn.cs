using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

public record CustomerDebtReducedForReturn(
    CustomerId CustomerId,
    Guid       SaleReturnId,
    decimal    ReducedAmount,
    decimal    OutstandingDebtAfter,
    string     ReducedBy,
    DateTime   OccurredAt) : IDomainEvent;
