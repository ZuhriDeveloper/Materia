using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;

namespace Materia.Application.Queries.Purchasing;

/// <summary>
/// The PO read model stores only a ProductId per line; fills in the current product
/// name for display, resolving every distinct product across the orders in one lookup.
/// </summary>
internal static class PurchaseOrderProductNames
{
    public static async Task<IReadOnlyList<PurchaseOrderDto>> ApplyAsync(
        IReadOnlyList<PurchaseOrderDto> orders, IProductQueryRepository products, CancellationToken ct)
    {
        var ids = orders.SelectMany(o => o.Lines).Select(l => l.ProductId).Distinct().ToList();
        if (ids.Count == 0) return orders;

        var names = await products.GetNamesAsync(ids, ct);

        return orders
            .Select(o => o with
            {
                Lines = o.Lines
                    .Select(l => l with { ProductName = names.GetValueOrDefault(l.ProductId) ?? l.ProductName })
                    .ToList(),
            })
            .ToList();
    }
}
