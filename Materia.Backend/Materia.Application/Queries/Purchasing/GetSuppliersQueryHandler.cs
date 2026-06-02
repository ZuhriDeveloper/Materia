using Materia.Application.Contracts.Purchasing;

namespace Materia.Application.Queries.Purchasing;

public sealed class GetSuppliersQueryHandler(ISupplierQueryRepository repository)
{
    public Task<IReadOnlyList<SupplierDto>> HandleAsync(bool activeOnly = false, CancellationToken ct = default)
        => repository.GetAllAsync(activeOnly, ct);
}
