using Materia.Domain.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
    Task SaveAsync(Product product, CancellationToken ct = default);
}
