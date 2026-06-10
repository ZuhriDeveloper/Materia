using Materia.Application.Contracts.Financials;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Financials.Queries;

public sealed record GetChangeFundQuery(int Page, int PageSize);

public record ChangeFundResultDto(
    IReadOnlyList<ChangeFundDepositDto> Items,
    int                                TotalCount,
    int                                Page,
    int                                PageSize,
    decimal                            TotalBalance)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class GetChangeFundQueryHandler(IChangeFundQueryRepository repository)
{
    public async Task<ChangeFundResultDto> HandleAsync(
        GetChangeFundQuery query, CancellationToken ct = default)
    {
        var paged   = await repository.GetPagedAsync(query.Page, query.PageSize, ct);
        var balance = await repository.GetTotalBalanceAsync(ct);

        return new ChangeFundResultDto(
            paged.Items,
            paged.TotalCount,
            paged.Page,
            paged.PageSize,
            balance);
    }
}

public sealed record GetChangeFundWithdrawalsQuery(int Page, int PageSize);

public sealed class GetChangeFundWithdrawalsQueryHandler(IChangeFundQueryRepository repository)
{
    public Task<PagedResult<ChangeFundWithdrawalDto>> HandleAsync(
        GetChangeFundWithdrawalsQuery query, CancellationToken ct = default)
        => repository.GetPagedWithdrawalsAsync(query.Page, query.PageSize, ct);
}
