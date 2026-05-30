using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

public record CustomerActivated(CustomerId CustomerId, string ActivatedBy, DateTime OccurredAt) : IDomainEvent;
