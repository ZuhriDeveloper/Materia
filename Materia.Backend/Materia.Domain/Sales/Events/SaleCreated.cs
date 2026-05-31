using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleCreated(
    SaleId   SaleId,
    string   ReferenceNo,
    string   CreatedBy,
    DateTime OccurredAt) : IDomainEvent;
