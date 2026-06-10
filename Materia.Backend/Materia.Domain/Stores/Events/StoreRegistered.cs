using Materia.Domain.Common;

namespace Materia.Domain.Stores.Events;

public record StoreRegistered(
    StoreId StoreId,
    string Name,
    string Code,
    string RegisteredBy,
    DateTime OccurredAt) : IDomainEvent;
