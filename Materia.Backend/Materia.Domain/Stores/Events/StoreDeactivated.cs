using Materia.Domain.Common;

namespace Materia.Domain.Stores.Events;

public record StoreDeactivated(
    StoreId StoreId,
    string DeactivatedBy,
    DateTime OccurredAt) : IDomainEvent;
