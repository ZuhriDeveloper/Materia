using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

public record CustomerUpdated(
    CustomerId CustomerId,
    string     Name,
    string     Phone,
    string?    Email,
    string     UpdatedBy,
    DateTime   OccurredAt) : IDomainEvent;
