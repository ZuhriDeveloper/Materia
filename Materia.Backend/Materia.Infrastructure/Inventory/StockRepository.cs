using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Stores;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;
using Materia.Domain.Purchasing;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class StockRepository(AppDbContext context, ICurrentStore currentStore) : IStockRepository
{
    private const string AggregateType = "Stock";

    public async Task<Stock?> GetAsync(
        ProductId productId, VariantId? variantId = null, CancellationToken ct = default)
    {
        var query = context.StockReadModels.Where(s => s.ProductId == productId.Value);
        // Null-safe variant match: a null parameter must translate to "IS NULL", not "= @p".
        query = variantId is null
            ? query.Where(s => s.VariantId == null)
            : query.Where(s => s.VariantId == variantId.Value);

        var stockId = await query.Select(s => s.Id).FirstOrDefaultAsync(ct);

        if (stockId == Guid.Empty) return null;

        var storeId = currentStore.StoreId;
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == stockId && e.AggregateType == AggregateType && e.StoreId == storeId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        var events = stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData));
        return Stock.Reconstitute(events);
    }

    public async Task SaveAsync(Stock stock, CancellationToken ct = default)
    {
        var newEvents = stock.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = stock.Version - newEvents.Count;
        var storeId = currentStore.StoreId;

        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                StoreId = storeId,
                AggregateType = AggregateType,
                AggregateId = stock.Id.Value,
                Version = baseVersion + i + 1,
                EventType = EventTypeRegistry.GetName(evt),
                EventData = EventSerializer.Serialize(evt),
                OccurredAt = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(stock, newEvents, ct);
        await context.SaveChangesAsync(ct);
        stock.ClearDomainEvents();
    }

    private async Task UpdateProjectionAsync(
        Stock stock,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        CancellationToken ct)
    {
        var variantId = stock.VariantId?.Value;
        var projection = await context.StockReadModels.FirstOrDefaultAsync(
            s => s.ProductId == stock.ProductId.Value && s.VariantId == variantId, ct);

        if (projection is null)
        {
            projection = new StockReadModel
            {
                Id = stock.Id.Value,
                StoreId = currentStore.StoreId,
                ProductId = stock.ProductId.Value,
                VariantId = variantId,
                Unit = stock.Unit,
            };
            context.StockReadModels.Add(projection);
        }

        projection.Quantity = stock.Quantity;
        projection.Unit = stock.Unit;

        var lastAdjust = newEvents.OfType<StockAdjusted>().LastOrDefault();
        if (lastAdjust is not null)
        {
            projection.LastAdjustedAt = lastAdjust.OccurredAt;
            projection.LastAdjustedBy = lastAdjust.AdjustedBy;
        }

        var lastReconcile = newEvents.OfType<StockReconciledFromPurchase>().LastOrDefault();
        if (lastReconcile is not null)
        {
            projection.LastAdjustedAt = lastReconcile.OccurredAt;
            projection.LastAdjustedBy = lastReconcile.ReconciledBy;
        }
    }
}
