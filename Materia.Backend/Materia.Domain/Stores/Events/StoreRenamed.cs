using Materia.Domain.Common;

namespace Materia.Domain.Stores.Events;

public record StoreRenamed(
    StoreId StoreId,
    string Name,
    string RenamedBy,
    DateTime OccurredAt) : IDomainEvent;
