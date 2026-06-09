namespace Materia.Application.Contracts.Customers;

/// <summary>
/// A previously recorded receivable payment, looked up by its client idempotency key.
/// Carries exactly what is needed to replay the original API response on a duplicate
/// submission (the prior PaymentId, resulting balance, and FIFO allocations).
/// </summary>
public sealed record StoredReceivablePayment(
    Guid                                       PaymentId,
    decimal                                    NewBalance,
    IReadOnlyList<StoredReceivableAllocation>  Allocations);

public sealed record StoredReceivableAllocation(
    Guid    SaleId,
    string  ReferenceNo,
    decimal AppliedAmount,
    decimal RemainingAfter);
