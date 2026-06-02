using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record SupplierRegistered(
    SupplierId SupplierId,
    string Name,
    string? ContactPhone,
    string CreatedBy,
    DateTime OccurredAt) : IDomainEvent;
