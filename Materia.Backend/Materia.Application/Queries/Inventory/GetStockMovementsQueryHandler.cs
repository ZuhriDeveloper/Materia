using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Inventory;

public record GetStockMovementsQuery(Guid ProductId, DateTime? From = null, DateTime? To = null);

/// <summary>Returns the stock flow (kartu stok) for a product, optionally within a date window.</summary>
public class GetStockMovementsQueryHandler(IStockMovementQueryRepository repository)
{
    public Task<IReadOnlyList<StockMovementDto>> HandleAsync(
        GetStockMovementsQuery query, CancellationToken ct = default)
        => repository.GetMovementsAsync(query.ProductId, query.From, query.To, ct);
}
