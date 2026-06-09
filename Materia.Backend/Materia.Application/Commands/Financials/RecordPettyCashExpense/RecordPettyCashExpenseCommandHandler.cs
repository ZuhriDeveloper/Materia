using Materia.Application.Contracts.Financials;
using Materia.Domain.Financials;

namespace Materia.Application.Commands.Financials.RecordPettyCashExpense;

public class RecordPettyCashExpenseCommandHandler(
    IPettyCashRepository          repository,
    IPettyCashReferenceGenerator  referenceGenerator)
{
    public async Task<Guid> HandleAsync(
        RecordPettyCashExpenseCommand command, CancellationToken ct = default)
    {
        var referenceNo = await referenceGenerator.GenerateAsync(ct);

        var expense = PettyCashExpense.Record(
            command.Amount,
            command.Recipient,
            command.Category,
            command.ReasonDetail,
            command.Notes,
            referenceNo,
            command.RecordedBy);

        await repository.SaveAsync(expense, ct);
        return expense.Id.Value;
    }
}
