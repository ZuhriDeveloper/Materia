using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record SupplierRegistered(
    SupplierId SupplierId,
    string Name,
    string? ContactPhone,
    string? Description,
    string? SalesmanName,
    string? SalesmanPhone,
    string CreatedBy,
    DateTime OccurredAt) : IDomainEvent;
