using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Inventory;

public record GetStockByProductIdQuery(Guid ProductId);

public class GetStockByProductIdQueryHandler(IStockQueryRepository repository)
{
    public Task<StockDto?> HandleAsync(GetStockByProductIdQuery query, CancellationToken ct = default)
        => repository.GetByProductIdAsync(query.ProductId, ct);
}

public record GetProductStocksQuery(Guid ProductId);

/// <summary>Returns all stock rows for a product, including one per color variant.</summary>
public class GetProductStocksQueryHandler(IStockQueryRepository repository)
{
    public Task<IReadOnlyList<StockDto>> HandleAsync(
        GetProductStocksQuery query, CancellationToken ct = default)
        => repository.GetByProductAsync(query.ProductId, ct);
}
