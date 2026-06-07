using Materia.Domain.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IStockRepository
{
    /// <summary>
    /// Loads the stock for a product, or for a specific color variant when
    /// <paramref name="variantId"/> is provided (null = product-level stock).
    /// </summary>
    Task<Stock?> GetAsync(
        ProductId productId, VariantId? variantId = null, CancellationToken ct = default);

    Task SaveAsync(Stock stock, CancellationToken ct = default);
}
