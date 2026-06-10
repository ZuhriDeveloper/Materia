using Materia.Domain.Common;

namespace Materia.Domain.Stores.Events;

public record StoreActivated(
    StoreId StoreId,
    string ActivatedBy,
    DateTime OccurredAt) : IDomainEvent;
