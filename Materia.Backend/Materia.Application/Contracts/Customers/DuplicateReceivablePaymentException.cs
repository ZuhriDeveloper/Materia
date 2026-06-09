namespace Materia.Application.Contracts.Customers;

/// <summary>
/// Thrown by the customer repository when persisting a receivable payment violates the
/// unique constraint on the idempotency key — i.e. a concurrent request with the same key
/// committed first. The application layer catches this (without depending on any specific
/// persistence technology) and replays the winning request's result instead of failing.
/// </summary>
public sealed class DuplicateReceivablePaymentException(Guid idempotencyKey)
    : Exception($"A receivable payment with idempotency key '{idempotencyKey}' already exists.")
{
    public Guid IdempotencyKey { get; } = idempotencyKey;
}
