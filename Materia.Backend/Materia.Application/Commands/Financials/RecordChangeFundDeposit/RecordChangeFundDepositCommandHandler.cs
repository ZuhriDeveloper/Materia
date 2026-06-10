using Materia.Application.Contracts.Financials;
using Materia.Domain.Financials;

namespace Materia.Application.Commands.Financials.RecordChangeFundDeposit;

public class RecordChangeFundDepositCommandHandler(IChangeFundRepository repository)
{
    public async Task<Guid> HandleAsync(
        RecordChangeFundDepositCommand command, CancellationToken ct = default)
    {
        // Idempotency guard (1/2): a retried or double-submitted request carries the same
        // client key. The deposit ledger is append-only, so a duplicate would permanently
        // inflate the balance — replay the prior result instead of recording again.
        var prior = await repository.FindDepositIdByKeyAsync(command.IdempotencyKey, ct);
        if (prior is not null)
            return prior.Value;

        var deposit = ChangeFundDeposit.Record(
            command.Amount,
            ChangeFundSource.ManualEntry,
            null,
            command.Notes,
            command.RecordedBy,
            command.IdempotencyKey);

        try
        {
            await repository.SaveAsync(deposit, ct);
        }
        catch (DuplicateChangeFundEntryException)
        {
            // Idempotency guard (2/2): a concurrent request with the same key won the race
            // and committed first; the unique index rejected ours (and rolled back the event
            // append, so no double deposit was persisted). Replay the winner's result.
            var winner = await repository.FindDepositIdByKeyAsync(command.IdempotencyKey, ct);
            if (winner is not null)
                return winner.Value;
            throw;
        }

        return deposit.Id.Value;
    }
}
