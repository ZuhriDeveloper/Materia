using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Inventory;

public record GetStockByProductIdQuery(Guid ProductId);

public class GetStockByProductIdQueryHandler(IStockQueryRepository repository)
{
    public Task<StockDto?> HandleAsync(GetStockByProductIdQuery query, CancellationToken ct = default)
        => repository.GetByProductIdAsync(query.ProductId, ct);
}
