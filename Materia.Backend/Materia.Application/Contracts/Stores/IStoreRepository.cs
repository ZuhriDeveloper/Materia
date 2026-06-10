using Materia.Domain.Stores;

namespace Materia.Application.Contracts.Stores;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(StoreId id, CancellationToken ct = default);
    Task SaveAsync(Store store, CancellationToken ct = default);
}
