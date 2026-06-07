using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IStockQueryRepository
{
    /// <summary>Product-level stock (variant-less). Returns null if none exists.</summary>
    Task<StockDto?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);

    /// <summary>All stock rows for a product, including one per color variant.</summary>
    Task<IReadOnlyList<StockDto>> GetByProductAsync(Guid productId, CancellationToken ct = default);
}
