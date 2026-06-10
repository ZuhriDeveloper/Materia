namespace Materia.Application.Contracts.Financials;

/// <summary>
/// Thrown by the petty cash repository when persisting an expense violates the unique
/// constraint on the idempotency key — i.e. a concurrent request with the same key
/// committed first. The application layer catches this (without depending on any specific
/// persistence technology) and replays the winning request's result instead of recording
/// the expense (and any TukarUangKembalian change-fund deposit) twice.
/// </summary>
public sealed class DuplicatePettyCashExpenseException(Guid idempotencyKey)
    : Exception($"A petty cash expense with idempotency key '{idempotencyKey}' already exists.")
{
    public Guid IdempotencyKey { get; } = idempotencyKey;
}
