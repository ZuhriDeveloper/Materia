using Materia.Domain.Common;

namespace Materia.Domain.Purchasing.Events;

public record SupplierActivated(
    SupplierId SupplierId,
    string ActivatedBy,
    DateTime OccurredAt) : IDomainEvent;
