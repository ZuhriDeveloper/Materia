using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class StockQueryRepository(AppDbContext context) : IStockQueryRepository
{
    public async Task<StockDto?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        var s = await context.StockReadModels
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.VariantId == null, ct);

        return s is null ? null : new StockDto(
            s.ProductId, s.Quantity, s.Unit, s.LastAdjustedAt, s.LastAdjustedBy);
    }

    public async Task<IReadOnlyList<StockDto>> GetByProductAsync(
        Guid productId, CancellationToken ct = default)
    {
        var rows = await context.StockReadModels
            .Where(x => x.ProductId == productId)
            .ToListAsync(ct);

        if (rows.Count == 0) return [];

        // Resolve color names for the variant rows in a single lookup.
        var variantIds = rows.Where(r => r.VariantId.HasValue).Select(r => r.VariantId!.Value).ToList();
        var colorByVariant = variantIds.Count == 0
            ? []
            : await context.ProductVariantReadModels
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.ColorName, ct);

        return rows
            .Select(s => new StockDto(
                s.ProductId, s.Quantity, s.Unit, s.LastAdjustedAt, s.LastAdjustedBy,
                s.VariantId,
                s.VariantId is { } vid && colorByVariant.TryGetValue(vid, out var name) ? name : null))
            .ToList();
    }
}
