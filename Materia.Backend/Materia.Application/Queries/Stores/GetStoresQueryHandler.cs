using Materia.Application.Contracts.Stores;
using Materia.Application.DTOs.Stores;

namespace Materia.Application.Queries.Stores;

public record GetStoresQuery;

public record GetStoreByIdQuery(Guid Id);

public class GetStoresQueryHandler(IStoreQueryRepository repository)
{
    public Task<IReadOnlyList<StoreDto>> HandleAsync(GetStoresQuery query, CancellationToken ct = default)
        => repository.GetAllAsync(ct);

    public Task<StoreDto?> HandleByIdAsync(GetStoreByIdQuery query, CancellationToken ct = default)
        => repository.GetByIdAsync(query.Id, ct);
}
