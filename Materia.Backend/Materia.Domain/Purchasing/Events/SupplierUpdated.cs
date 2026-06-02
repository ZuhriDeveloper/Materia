using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record SupplierUpdated(
    SupplierId SupplierId,
    string Name,
    string? ContactPhone,
    string UpdatedBy,
    DateTime OccurredAt) : IDomainEvent;
