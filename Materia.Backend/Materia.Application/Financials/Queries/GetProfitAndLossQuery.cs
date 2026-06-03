using Materia.Application.Contracts.Financials;

namespace Materia.Application.Financials.Queries;

public sealed record GetProfitAndLossQuery(DateTime From, DateTime To);

public sealed class GetProfitAndLossQueryHandler(IFinancialQueryRepository repository)
{
    public Task<ProfitAndLossDto> HandleAsync(
        GetProfitAndLossQuery query, CancellationToken ct = default)
    {
        if (query.From > query.To)
            throw new ArgumentException("From date must not be after To date.", nameof(query));

        // Normalise to full calendar days
        var from = query.From.Date;
        var to   = query.To.Date.AddDays(1).AddTicks(-1);

        return repository.GetProfitAndLossAsync(from, to, ct);
    }
}
