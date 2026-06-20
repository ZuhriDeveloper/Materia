using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Stores;
using Materia.Application.DTOs.Inventory;
using Materia.Application.Queries.Inventory;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class StockMovementQueryRepository(AppDbContext context, ICurrentStore currentStore)
    : IStockMovementQueryRepository
{
    private const string AggregateType = "Stock";

    public async Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(
        Guid productId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        // Every stock bucket for the product: product-level (null variant) + one per color.
        var buckets = await context.StockReadModels
            .Where(s => s.ProductId == productId)
            .Select(s => new { s.Id, s.VariantId })
            .ToListAsync(ct);

        if (buckets.Count == 0) return [];

        // Resolve color names for the variant buckets in a single lookup.
        var variantIds = buckets.Where(b => b.VariantId.HasValue).Select(b => b.VariantId!.Value).ToList();
        var colorByVariant = variantIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await context.ProductVariantReadModels
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.ColorName, ct);

        var bucketIds = buckets.Select(b => b.Id).ToList();
        var storeId = currentStore.StoreId;

        // The event store is not store-filtered globally — scope explicitly (defense in depth).
        var stored = await context.StoredEvents
            .Where(e => e.AggregateType == AggregateType
                        && bucketIds.Contains(e.AggregateId)
                        && e.StoreId == storeId)
            .OrderBy(e => e.AggregateId).ThenBy(e => e.Version)
            .ToListAsync(ct);

        var movements = new List<StockMovementDto>();
        foreach (var bucket in buckets)
        {
            var events = stored
                .Where(e => e.AggregateId == bucket.Id)
                .Select(e => EventSerializer.Deserialize(e.EventType, e.EventData));
            var colorName = bucket.VariantId is { } vid && colorByVariant.TryGetValue(vid, out var name)
                ? name
                : null;
            movements.AddRange(StockMovementMapper.Map(events, bucket.VariantId, colorName));
        }

        IEnumerable<StockMovementDto> result = movements;
        if (from is { } f) result = result.Where(m => m.OccurredAt >= f);
        if (to is { } t) result = result.Where(m => m.OccurredAt <= t);

        return result.OrderByDescending(m => m.OccurredAt).ToList();
    }
}
