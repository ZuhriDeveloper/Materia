using Materia.Domain.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IUnitRepository
{
    Task<Unit?> GetByIdAsync(UnitId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(UnitId id, CancellationToken ct = default);
    Task SaveAsync(Unit unit, CancellationToken ct = default);
}
