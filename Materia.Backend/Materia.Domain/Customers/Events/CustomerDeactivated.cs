using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

public record CustomerDeactivated(CustomerId CustomerId, string DeactivatedBy, DateTime OccurredAt) : IDomainEvent;
