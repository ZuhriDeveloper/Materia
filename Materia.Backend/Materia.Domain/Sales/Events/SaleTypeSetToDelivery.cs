using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleTypeSetToDelivery(
    SaleId   SaleId,
    Guid     CustomerAddressId,
    string   DeliveryAddress,
    string   UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
