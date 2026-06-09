using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

/// <summary>
/// Raised when a credit (piutang / bon) sale adds to a customer's outstanding debt balance.
/// </summary>
public record CustomerDebtIncurred(
    CustomerId CustomerId,
    Guid       SaleId,
    string     ReferenceNo,
    decimal    Amount,
    decimal    NewBalance,
    string     IncurredBy,
    DateTime   OccurredAt) : IDomainEvent;
