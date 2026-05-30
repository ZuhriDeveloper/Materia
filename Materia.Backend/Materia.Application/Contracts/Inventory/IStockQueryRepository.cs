using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IStockQueryRepository
{
    Task<StockDto?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
}
