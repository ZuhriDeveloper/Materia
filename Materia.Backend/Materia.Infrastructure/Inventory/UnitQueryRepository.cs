using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class UnitQueryRepository(AppDbContext context) : IUnitQueryRepository
{
    public async Task<IReadOnlyList<UnitDto>> GetAllAsync(CancellationToken ct = default)
    {
        var models = await context.UnitReadModels
            .OrderBy(u => u.Name)
            .ToListAsync(ct);

        return models.Select(u => new UnitDto(
            u.Id, u.Name, u.Symbol, u.IsActive,
            u.CreatedBy, u.CreatedAt, u.UpdatedBy, u.UpdatedAt)).ToList();
    }

    public async Task<UnitDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var u = await context.UnitReadModels.FindAsync([id], ct);
        return u is null ? null : new UnitDto(
            u.Id, u.Name, u.Symbol, u.IsActive,
            u.CreatedBy, u.CreatedAt, u.UpdatedBy, u.UpdatedAt);
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        return excludeId.HasValue
            ? context.UnitReadModels.AnyAsync(u => u.Name == trimmed && u.Id != excludeId.Value, ct)
            : context.UnitReadModels.AnyAsync(u => u.Name == trimmed, ct);
    }
}
