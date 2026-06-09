using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

/// <summary>
/// A settlement recorded against a finalized sale. Supports both full payment and a
/// partial payment (down payment / DP), in which case <see cref="OutstandingAmount"/> is
/// the remaining customer debt (piutang / bon).
/// </summary>
public record SaleSettled(
    SaleId        SaleId,
    decimal       AmountPaid,
    decimal       Change,
    decimal       OutstandingAmount,
    PaymentMethod Method,
    bool          IsCredit,
    DateTime      PaidAt,
    DateTime      OccurredAt) : IDomainEvent;
