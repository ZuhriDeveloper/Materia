using Materia.Application.Contracts.Customers;
using Materia.Domain.Common;
using Materia.Domain.Customers;
using Materia.Domain.Customers.Events;

namespace Materia.Application.Commands.Customers.RecordReceivablePayment;

public class RecordReceivablePaymentCommandHandler(ICustomerRepository repository)
{
    public async Task<RecordReceivablePaymentResult> HandleAsync(
        RecordReceivablePaymentCommand command, CancellationToken ct = default)
    {
        // Idempotency guard (1/2): a retried or double-submitted request carries the same
        // client key. If we already recorded a payment for it, replay the prior result rather
        // than collecting again. This covers the sequential-resend case (security finding M1).
        var prior = await repository.FindReceivablePaymentByKeyAsync(command.IdempotencyKey, ct);
        if (prior is not null)
            return ToResult(prior);

        var customer = await repository.GetByIdAsync(
            new CustomerId(command.CustomerId), ct)
            ?? throw new DomainException("Pelanggan tidak ditemukan.");

        var paymentId = customer.RecordRepayment(
            command.Amount, command.Method, command.Notes, command.ReceivedBy,
            command.IdempotencyKey);

        // Capture the baked allocations from the raised event BEFORE SaveAsync clears domain events.
        var raised = customer.DomainEvents.OfType<ReceivablePaymentRecorded>().Single();
        var allocations = raised.Allocations
            .Select(a => new ReceivableAllocationResult(
                a.SaleId, a.ReferenceNo, a.AppliedAmount, a.RemainingAfter))
            .ToList()
            .AsReadOnly();
        var newBalance = raised.NewBalance;

        try
        {
            await repository.SaveAsync(customer, ct);
        }
        catch (DuplicateReceivablePaymentException)
        {
            // Idempotency guard (2/2): a concurrent request with the same key won the race and
            // committed first; the unique index rejected ours (and rolled back the event append,
            // so no double payment was persisted). Replay the winner's result.
            var winner = await repository.FindReceivablePaymentByKeyAsync(command.IdempotencyKey, ct);
            if (winner is not null)
                return ToResult(winner);
            throw;
        }

        return new RecordReceivablePaymentResult(paymentId, newBalance, allocations);
    }

    private static RecordReceivablePaymentResult ToResult(StoredReceivablePayment payment) =>
        new(payment.PaymentId,
            payment.NewBalance,
            payment.Allocations
                .Select(a => new ReceivableAllocationResult(
                    a.SaleId, a.ReferenceNo, a.AppliedAmount, a.RemainingAfter))
                .ToList()
                .AsReadOnly());
}
