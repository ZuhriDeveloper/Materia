namespace Materia.Application.Contracts.Financials;

/// <summary>
/// Thrown by the change fund repository when persisting a deposit or withdrawal violates
/// the unique constraint on the idempotency key — i.e. a concurrent request with the same
/// key committed first. The application layer catches this (without depending on any
/// specific persistence technology) and replays the winning request's result instead of
/// appending a duplicate event to the append-only ledger.
/// </summary>
public sealed class DuplicateChangeFundEntryException(Guid idempotencyKey)
    : Exception($"A change fund entry with idempotency key '{idempotencyKey}' already exists.")
{
    public Guid IdempotencyKey { get; } = idempotencyKey;
}
