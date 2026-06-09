using Materia.Application.Contracts.Customers;
using Materia.Application.DTOs.Customers;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Customers;

public record GetOutstandingReceivablesQuery(int Page, int PageSize, string? Search);

public class GetOutstandingReceivablesQueryHandler(ICustomerQueryRepository repository)
{
    public Task<PagedResult<ReceivableSummaryDto>> HandleAsync(
        GetOutstandingReceivablesQuery query, CancellationToken ct = default)
    {
        // Bound paging so a caller cannot pull the entire debtor list (PII) in one request
        // or force a negative OFFSET. Mirrors the existing PettyCash query convention.
        var page     = Math.Max(query.Page, 1);
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        return repository.GetOutstandingReceivablesAsync(page, pageSize, query.Search, ct);
    }
}
