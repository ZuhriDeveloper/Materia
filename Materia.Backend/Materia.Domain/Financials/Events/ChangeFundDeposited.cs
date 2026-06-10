using Materia.Domain.Common;

namespace Materia.Domain.Financials.Events;

/// <summary>
/// Raised when cash is deposited into the change fund (uang kembalian) —
/// either entered manually by an admin or exchanged from petty cash.
/// <paramref name="IdempotencyKey"/> is the client-supplied de-duplication token,
/// baked in so the projection's unique index can reject duplicate submissions.
/// </summary>
public record ChangeFundDeposited(
    ChangeFundDepositId Id,
    decimal             Amount,
    ChangeFundSource    Source,
    string?             SourceReferenceNo,
    string?             Notes,
    string              RecordedBy,
    DateTime            OccurredAt,
    Guid                IdempotencyKey) : IDomainEvent;
