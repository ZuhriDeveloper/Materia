using Materia.Domain.Financials;

namespace Materia.Application.Commands.Financials.RecordPettyCashExpense;

public record RecordPettyCashExpenseCommand(
    decimal           Amount,
    string            Recipient,
    PettyCashCategory Category,
    string?           ReasonDetail,
    string?           Notes,
    string            RecordedBy);
