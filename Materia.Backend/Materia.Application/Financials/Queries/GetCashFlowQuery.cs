using Materia.Application.Contracts.Financials;

namespace Materia.Application.Financials.Queries;

public sealed record GetCashFlowQuery(DateTime From, DateTime To);

public sealed class GetCashFlowQueryHandler(IFinancialQueryRepository repository)
{
    public Task<CashFlowDto> HandleAsync(
        GetCashFlowQuery query, CancellationToken ct = default)
    {
        if (query.From > query.To)
            throw new ArgumentException("From date must not be after To date.", nameof(query));

        var from = query.From.Date;
        var to   = query.To.Date.AddDays(1).AddTicks(-1);

        return repository.GetCashFlowAsync(from, to, ct);
    }
}
