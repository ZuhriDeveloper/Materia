using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IUnitQueryRepository
{
    Task<IReadOnlyList<UnitDto>> GetAllAsync(CancellationToken ct = default);
    Task<UnitDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
