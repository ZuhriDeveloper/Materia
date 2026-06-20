using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IStockMovementQueryRepository
{
    /// <summary>
    /// The chronological flow of stock movements for a product across all buckets (product-level
    /// and every color variant), newest first. Optionally limited to the inclusive
    /// [<paramref name="from"/>, <paramref name="to"/>] window on the movement timestamp (UTC).
    /// </summary>
    Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(
        Guid productId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}
