using Materia.Application.Contracts.Purchasing;

namespace Materia.Application.Queries.Purchasing;

public sealed class GetPurchaseOrderByIdQueryHandler(IPurchaseOrderQueryRepository repository)
{
    public Task<PurchaseOrderDto?> HandleAsync(Guid id, CancellationToken ct = default)
        => repository.GetByIdAsync(id, ct);
}
