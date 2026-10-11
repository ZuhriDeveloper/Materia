using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;

namespace Materia.Application.Queries.Purchasing;

public sealed class GetPurchaseOrderByIdQueryHandler(
    IPurchaseOrderQueryRepository repository,
    IProductQueryRepository       products)
{
    public async Task<PurchaseOrderDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var order = await repository.GetByIdAsync(id, ct);
        if (order is null) return null;

        return (await PurchaseOrderProductNames.ApplyAsync([order], products, ct))[0];
    }
}
