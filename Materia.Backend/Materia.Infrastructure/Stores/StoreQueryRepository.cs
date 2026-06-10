using Materia.Application.Contracts.Stores;
using Materia.Application.DTOs.Stores;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Stores;

/// <summary>
/// Read side for the store registry. <see cref="StoreReadModel"/> is platform-level and
/// carries no per-store query filter, so SuperAdmin sees every store without needing
/// IgnoreQueryFilters().
/// </summary>
public class StoreQueryRepository(AppDbContext context) : IStoreQueryRepository
{
    public async Task<StoreDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.StoreReadModels
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StoreDto(s.Id, s.Name, s.Code, s.IsActive))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<StoreDto>> GetAllAsync(CancellationToken ct = default)
        => await context.StoreReadModels
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .Select(s => new StoreDto(s.Id, s.Name, s.Code, s.IsActive))
            .ToListAsync(ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
        => context.StoreReadModels.AnyAsync(s => s.Code == code, ct);

    public async Task<StoreParametersDto?> GetProfileByIdAsync(Guid id, CancellationToken ct = default)
        => await context.StoreReadModels
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StoreParametersDto(
                s.Id,
                s.Name,
                s.Code,
                s.IsActive,
                s.Address,
                s.Phone,
                s.MaxDeliveryDistanceKm))
            .FirstOrDefaultAsync(ct);
}
