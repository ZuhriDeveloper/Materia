using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

public record CustomerCreated(
    CustomerId CustomerId,
    string     Name,
    string     Phone,
    string?    Email,
    string     CreatedBy,
    DateTime   OccurredAt) : IDomainEvent;
