using Materia.Domain.Common;

namespace Materia.Domain.Financials.Events;

/// <summary>
/// Raised when cash is taken out of the change fund (uang kembalian) — the Admin-only
/// compensating entry for correcting an erroneous deposit (or returning excess float).
/// The deposit ledger is append-only, so corrections are recorded as withdrawals rather
/// than by mutating or deleting past events; balance = SUM(deposits) − SUM(withdrawals).
/// <paramref name="IdempotencyKey"/> is the client-supplied de-duplication token,
/// baked in so the projection's unique index can reject duplicate submissions.
/// </summary>
public record ChangeFundWithdrawn(
    ChangeFundWithdrawalId Id,
    decimal                Amount,
    string                 Reason,
    string                 RecordedBy,
    DateTime               OccurredAt,
    Guid                   IdempotencyKey) : IDomainEvent;
