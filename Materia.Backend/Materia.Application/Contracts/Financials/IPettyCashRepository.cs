using Materia.Domain.Financials;

namespace Materia.Application.Contracts.Financials;

public interface IPettyCashRepository
{
    Task<PettyCashExpense?> GetByIdAsync(PettyCashExpenseId id, CancellationToken ct = default);
    Task SaveAsync(PettyCashExpense expense, CancellationToken ct = default);
}
