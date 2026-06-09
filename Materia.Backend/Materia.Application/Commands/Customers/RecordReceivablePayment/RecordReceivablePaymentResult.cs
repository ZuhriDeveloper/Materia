namespace Materia.Application.Commands.Customers.RecordReceivablePayment;

public record ReceivableAllocationResult(
    Guid    SaleId,
    string  ReferenceNo,
    decimal AppliedAmount,
    decimal RemainingAfter);

public record RecordReceivablePaymentResult(
    Guid                               PaymentId,
    decimal                            NewBalance,
    IReadOnlyList<ReceivableAllocationResult> Allocations);
