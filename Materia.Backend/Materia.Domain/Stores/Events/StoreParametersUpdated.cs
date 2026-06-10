using Materia.Domain.Common;

namespace Materia.Domain.Stores.Events;

public record StoreParametersUpdated(
    StoreId StoreId,
    string Address,
    string Phone,
    decimal MaxDeliveryDistanceKm,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
