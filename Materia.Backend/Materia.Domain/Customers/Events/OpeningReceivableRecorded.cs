using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

/// <summary>
/// Raised when a pre-existing debt (saldo awal piutang) is recorded for a customer without a
/// sale behind it — typically legacy bon carried over from before Materia went live.
/// </summary>
/// <param name="ReceivableId">
/// Client-generated id for this receivable line. Stands in for the sale id in FIFO allocations,
/// and doubles as the double-submit guard (the aggregate rejects a repeated id).
/// </param>
/// <param name="IncurredAt">Original debt date; drives FIFO order. Distinct from OccurredAt (entry time).</param>
public record OpeningReceivableRecorded(
    CustomerId CustomerId,
    Guid       ReceivableId,
    string     ReferenceNo,
    decimal    Amount,
    DateTime   IncurredAt,
    decimal    NewBalance,
    string?    Notes,
    string     RecordedBy,
    DateTime   OccurredAt) : IDomainEvent;
