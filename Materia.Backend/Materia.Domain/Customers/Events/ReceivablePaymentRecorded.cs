using Materia.Domain.Common;
using Materia.Domain.Sales;

namespace Materia.Domain.Customers.Events;

/// <summary>
/// Raised when a cash collection is recorded against a customer's open receivables.
/// Allocations are baked in at command time (FIFO, oldest-first) so event replay is
/// always deterministic — no "now"-dependent recomputation during Apply.
/// </summary>
/// <param name="PaymentId">Server-issued surrogate id for this collection.</param>
/// <param name="IdempotencyKey">
/// Client-supplied de-duplication token. A retried or double-submitted request carries the
/// same key; the read-model projection enforces a unique constraint on it so a sequential
/// resend or concurrent race can never record a second payment. Distinct from PaymentId.
/// </param>
public record ReceivablePaymentRecorded(
    CustomerId                      CustomerId,
    Guid                            PaymentId,
    Guid                            IdempotencyKey,
    decimal                         Amount,
    decimal                         NewBalance,
    IReadOnlyList<ReceivableAllocation> Allocations,
    PaymentMethod                   Method,
    string?                         Notes,
    string                          ReceivedBy,
    DateTime                        OccurredAt) : IDomainEvent;

/// <summary>
/// How much of a single invoice was covered by one repayment, and what remains.
/// Plain Guid — no custom strongly-typed-Id converter needed here.
/// </summary>
public record ReceivableAllocation(
    Guid    SaleId,
    string  ReferenceNo,
    decimal AppliedAmount,
    decimal RemainingAfter);
