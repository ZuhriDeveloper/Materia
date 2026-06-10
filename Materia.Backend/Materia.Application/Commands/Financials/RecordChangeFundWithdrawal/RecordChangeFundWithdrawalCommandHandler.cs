using Materia.Application.Contracts.Financials;
using Materia.Domain.Common;
using Materia.Domain.Financials;

namespace Materia.Application.Commands.Financials.RecordChangeFundWithdrawal;

public class RecordChangeFundWithdrawalCommandHandler(
    IChangeFundRepository      repository,
    IChangeFundQueryRepository queryRepository)
{
    public async Task<Guid> HandleAsync(
        RecordChangeFundWithdrawalCommand command, CancellationToken ct = default)
    {
        // Idempotency guard (1/2): a retried or double-submitted request carries the same
        // client key — replay the prior result instead of deducting the fund twice.
        var prior = await repository.FindWithdrawalIdByKeyAsync(command.IdempotencyKey, ct);
        if (prior is not null)
            return prior.Value;

        // The fund is physical cash in the drawer — it can never go negative. Checked here
        // because the record-only aggregate holds no balance knowledge. (Read-then-write,
        // so a concurrent withdrawal could still slip past; acceptable for a single-admin
        // correction flow, and the balance stays auditable either way.)
        var balance = await queryRepository.GetTotalBalanceAsync(ct);
        if (command.Amount > balance)
            throw new DomainException(
                $"Jumlah penarikan melebihi saldo uang kembalian saat ini (Rp {balance:N0}).");

        var withdrawal = ChangeFundWithdrawal.Record(
            command.Amount,
            command.Reason,
            command.RecordedBy,
            command.IdempotencyKey);

        try
        {
            await repository.SaveAsync(withdrawal, ct);
        }
        catch (DuplicateChangeFundEntryException)
        {
            // Idempotency guard (2/2): a concurrent request with the same key won the race
            // and committed first; the unique index rejected ours. Replay the winner.
            var winner = await repository.FindWithdrawalIdByKeyAsync(command.IdempotencyKey, ct);
            if (winner is not null)
                return winner.Value;
            throw;
        }

        return withdrawal.Id.Value;
    }
}
