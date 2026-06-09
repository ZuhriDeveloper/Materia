using Materia.Application.Contracts.Financials;
using Materia.Application.DTOs.Inventory;
using Materia.Domain.Financials;

namespace Materia.Application.Financials.Queries;

public sealed record GetPettyCashExpensesQuery(
    int                Page,
    int                PageSize,
    DateTime?          From,
    DateTime?          To,
    PettyCashCategory? Category);

public sealed class GetPettyCashExpensesQueryHandler(IPettyCashQueryRepository repository)
{
    public Task<PagedResult<PettyCashExpenseDto>> HandleAsync(
        GetPettyCashExpensesQuery query, CancellationToken ct = default)
    {
        if (query.From > query.To)
            throw new ArgumentException("From date must not be after To date.", nameof(query));

        // Normalise to full calendar days, matching GetCashFlowQuery.
        var from = query.From?.Date;
        var to   = query.To?.Date.AddDays(1).AddTicks(-1);

        return repository.GetPagedAsync(query.Page, query.PageSize, from, to, query.Category, ct);
    }
}
