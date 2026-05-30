using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

public record CustomerAddressRemoved(
    CustomerId CustomerId,
    AddressId  AddressId,
    string     UpdatedBy,
    DateTime   OccurredAt) : IDomainEvent;
