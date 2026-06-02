using Materia.Application.Contracts.Purchasing;

namespace Materia.Application.Queries.Purchasing;

public sealed class GetSupplierByIdQueryHandler(ISupplierQueryRepository repository)
{
    public Task<SupplierDto?> HandleAsync(Guid supplierId, CancellationToken ct = default)
        => repository.GetByIdAsync(supplierId, ct);
}
