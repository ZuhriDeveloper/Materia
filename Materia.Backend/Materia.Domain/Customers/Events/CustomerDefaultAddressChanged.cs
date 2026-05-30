using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

public record CustomerDefaultAddressChanged(
    CustomerId CustomerId,
    AddressId  NewDefaultAddressId,
    string     UpdatedBy,
    DateTime   OccurredAt) : IDomainEvent;
