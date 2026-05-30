using Materia.Domain.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(CategoryId id, CancellationToken ct = default);
    Task SaveAsync(Category category, CancellationToken ct = default);
}
