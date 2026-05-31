using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SalePaid(
    SaleId        SaleId,
    decimal       PaidAmount,
    decimal       Change,
    PaymentMethod PaymentMethod,
    DateTime      PaidAt,
    DateTime      OccurredAt) : IDomainEvent;
