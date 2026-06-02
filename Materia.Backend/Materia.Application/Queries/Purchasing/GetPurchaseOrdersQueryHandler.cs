using Materia.Application.Contracts.Purchasing;

namespace Materia.Application.Queries.Purchasing;

public sealed class GetPurchaseOrdersQueryHandler(IPurchaseOrderQueryRepository repository)
{
    public Task<IReadOnlyList<PurchaseOrderDto>> HandleAsync(CancellationToken ct = default)
        => repository.GetAllAsync(ct);
}
