using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface ICategoryQueryRepository
{
    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
}
