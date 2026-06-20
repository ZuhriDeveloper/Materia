using Materia.Application.Contracts.Inventory;
using Materia.Application.Services;
using Materia.Domain.Inventory.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

/// <summary>
/// Recomputes <see cref="Persistence.Projections.StockReadModel.AverageCost"/> for every stock
/// bucket by replaying its purchase receipts through <see cref="MovingAverageCost"/>. Used once to
/// seed the column for stock that pre-dates the moving-average feature; safe to re-run.
/// </summary>
public class StockCostBackfill(AppDbContext context) : IStockCostBackfill
{
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var rows = await context.StockReadModels.ToListAsync(ct);
        var updated = 0;

        foreach (var row in rows)
        {
            var stored = await context.StoredEvents
                .Where(e => e.AggregateType == "Stock"
                            && e.AggregateId == row.Id
                            && e.StoreId == row.StoreId)
                .OrderBy(e => e.Version)
                .ToListAsync(ct);

            var qty = 0m;
            var avg = 0m;
            foreach (var se in stored)
            {
                switch (EventSerializer.Deserialize(se.EventType, se.EventData))
                {
                    case StockInitialized e:
                        qty = e.Quantity;
                        break;
                    case StockReconciledFromPurchase e:
                        avg = MovingAverageCost.AfterReceipt(qty, avg, e.ReceivedQty, e.UnitCost);
                        qty = e.NewQuantity;
                        break;
                    case StockReducedFromPurchaseReturn e:
                        qty = e.NewQuantity;
                        break;
                    case StockAdjusted e:
                        qty = e.NewQuantity;
                        break;
                }
            }

            if (row.AverageCost != avg)
            {
                row.AverageCost = avg;
                updated++;
            }
        }

        if (updated > 0)
            await context.SaveChangesAsync(ct);

        return updated;
    }
}
