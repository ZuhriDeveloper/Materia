using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record SupplierDeactivated(
    SupplierId SupplierId,
    string DeactivatedBy,
    DateTime OccurredAt) : IDomainEvent;
