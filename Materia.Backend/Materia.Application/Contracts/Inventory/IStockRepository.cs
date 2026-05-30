using Materia.Domain.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IStockRepository
{
    Task<Stock?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default);
    Task SaveAsync(Stock stock, CancellationToken ct = default);
}
