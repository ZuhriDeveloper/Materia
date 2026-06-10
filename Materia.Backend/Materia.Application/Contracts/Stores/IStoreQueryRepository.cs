using Materia.Application.DTOs.Stores;

namespace Materia.Application.Contracts.Stores;

/// <summary>
/// Read side for the store registry. Implementations bypass the per-store query
/// filter (the registry is platform-level, visible to SuperAdmin across all stores).
/// </summary>
public interface IStoreQueryRepository
{
    Task<StoreDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StoreDto>> GetAllAsync(CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
}
