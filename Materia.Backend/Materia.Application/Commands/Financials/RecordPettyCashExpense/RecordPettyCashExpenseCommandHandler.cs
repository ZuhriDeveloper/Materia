using Materia.Application.Contracts.Common;
using Materia.Application.Contracts.Financials;
using Materia.Domain.Financials;

namespace Materia.Application.Commands.Financials.RecordPettyCashExpense;

public class RecordPettyCashExpenseCommandHandler(
    IPettyCashRepository         repository,
    IPettyCashReferenceGenerator referenceGenerator,
    IChangeFundRepository        changeFundRepository,
    IUnitOfWork                  unitOfWork)
{
    public async Task<Guid> HandleAsync(
        RecordPettyCashExpenseCommand command, CancellationToken ct = default)
    {
        // Idempotency guard (1/2): a retried or double-submitted request carries the same
        // client key. Each retry would otherwise mint a fresh reference number — a brand-new
        // expense (plus a duplicate change-fund deposit for TukarUangKembalian), so replay
        // the prior result instead of recording again.
        var prior = await repository.FindExpenseIdByKeyAsync(command.IdempotencyKey, ct);
        if (prior is not null)
            return prior.Value;

        var referenceNo = await referenceGenerator.GenerateAsync(ct);

        var expense = PettyCashExpense.Record(
            command.Amount,
            command.Recipient,
            command.Category,
            command.ReasonDetail,
            command.Notes,
            referenceNo,
            command.RecordedBy,
            command.IdempotencyKey);

        try
        {
            // The expense and its change-fund deposit are two ledgers describing the same
            // cash movement — they must commit atomically or reconciliation drifts.
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await repository.SaveAsync(expense, ct);

                // Side-effect: when this petty cash expense is for exchanging change fund money,
                // automatically create a ChangeFundDeposit so the change fund balance stays current.
                // Deliberately allowed for Cashier (unlike Admin-only manual deposits): the deposit
                // here is always backed 1:1 by this auditable petty cash expense — which is also why
                // it reuses the expense's idempotency key, so one retry dedupes both ledgers.
                if (command.Category == PettyCashCategory.TukarUangKembalian)
                {
                    var deposit = ChangeFundDeposit.Record(
                        command.Amount,
                        ChangeFundSource.PettyCashExchange,
                        expense.ReferenceNo,
                        command.Notes,
                        command.RecordedBy,
                        command.IdempotencyKey);

                    await changeFundRepository.SaveAsync(deposit, ct);
                }
            }, ct);
        }
        catch (Exception ex) when (ex is DuplicatePettyCashExpenseException
                                      or DuplicateChangeFundEntryException)
        {
            // Idempotency guard (2/2): a concurrent request with the same key won the race and
            // committed first; a unique index rejected ours and the transaction rolled back, so
            // neither ledger was written twice. Replay the winner's result.
            var winner = await repository.FindExpenseIdByKeyAsync(command.IdempotencyKey, ct);
            if (winner is not null)
                return winner.Value;
            throw;
        }

        return expense.Id.Value;
    }
}
