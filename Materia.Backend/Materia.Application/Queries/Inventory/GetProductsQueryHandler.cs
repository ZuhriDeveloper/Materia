using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Inventory;

public record GetProductsQuery(int Page = 1, int PageSize = 20, bool? IsActive = null);

public class GetProductsQueryHandler(IProductQueryRepository repository)
{
    public Task<PagedResult<ProductDto>> HandleAsync(GetProductsQuery query, CancellationToken ct = default)
        => repository.GetPagedAsync(query.Page, query.PageSize, query.IsActive, ct);
}
