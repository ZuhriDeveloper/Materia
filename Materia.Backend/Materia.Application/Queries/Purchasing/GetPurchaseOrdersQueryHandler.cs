using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;

namespace Materia.Application.Queries.Purchasing;

public sealed class GetPurchaseOrdersQueryHandler(
    IPurchaseOrderQueryRepository repository,
    IProductQueryRepository       products)
{
    public async Task<IReadOnlyList<PurchaseOrderDto>> HandleAsync(CancellationToken ct = default)
        => await PurchaseOrderProductNames.ApplyAsync(await repository.GetAllAsync(ct), products, ct);
}
