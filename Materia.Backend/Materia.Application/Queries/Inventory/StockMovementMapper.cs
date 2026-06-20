using Materia.Application.DTOs.Inventory;
using Materia.Application.Services;
using Materia.Domain.Common;
using Materia.Domain.Inventory.Events;

namespace Materia.Application.Queries.Inventory;

/// <summary>
/// Folds a single stock bucket's event stream (ordered oldest-first) into <see cref="StockMovementDto"/>
/// rows, tracking the running on-hand balance and weighted-average cost. Sales are distinguished from
/// manual adjustments by the <c>"Penjualan {ref}"</c> reason the sale workflow stamps on the deduction.
/// </summary>
public static class StockMovementMapper
{
    private const string SalePrefix = "Penjualan ";

    public static List<StockMovementDto> Map(
        IEnumerable<IDomainEvent> orderedEvents, Guid? variantId = null, string? colorName = null)
    {
        var rows = new List<StockMovementDto>();
        var qty = 0m;
        var avg = 0m;
        var unit = "";

        foreach (var e in orderedEvents)
        {
            switch (e)
            {
                case StockInitialized ev:
                    unit = ev.Unit;
                    qty = ev.Quantity;
                    Add(rows, ev.OccurredAt, StockMovementType.Initial, 0m, qty, unit,
                        reason: null, reference: null, by: ev.CreatedBy, unitCost: null,
                        avg, variantId, colorName);
                    break;

                case StockReconciledFromPurchase ev:
                    avg = MovingAverageCost.AfterReceipt(qty, avg, ev.ReceivedQty, ev.UnitCost);
                    qty = ev.NewQuantity;
                    unit = ev.Unit;
                    Add(rows, ev.OccurredAt, StockMovementType.PurchaseReceipt, ev.ReceivedQty, qty, unit,
                        reason: null, reference: ev.PurchaseOrderId.Value.ToString(), by: ev.ReconciledBy,
                        unitCost: ev.UnitCost, avg, variantId, colorName);
                    break;

                case StockReducedFromPurchaseReturn ev:
                    qty = ev.NewQuantity;
                    unit = ev.Unit;
                    Add(rows, ev.OccurredAt, StockMovementType.PurchaseReturn, -ev.ReturnedQty, qty, unit,
                        reason: null, reference: ev.PurchaseOrderId.Value.ToString(), by: ev.ReducedBy,
                        unitCost: ev.UnitCost, avg, variantId, colorName);
                    break;

                case StockAdjusted ev:
                    qty = ev.NewQuantity;
                    var isSale = ev.Reason is not null &&
                                 ev.Reason.StartsWith(SalePrefix, StringComparison.OrdinalIgnoreCase);
                    Add(rows, ev.OccurredAt,
                        isSale ? StockMovementType.Sale : StockMovementType.Adjustment,
                        ev.Delta, qty, unit,
                        reason: isSale ? null : ev.Reason,
                        reference: isSale ? ev.Reason![SalePrefix.Length..].Trim() : null,
                        by: ev.AdjustedBy, unitCost: null, avg, variantId, colorName);
                    break;

                case StockUnitCorrected ev:
                    unit = ev.Unit;
                    Add(rows, ev.OccurredAt, StockMovementType.UnitCorrection, 0m, qty, unit,
                        reason: null, reference: null, by: ev.CorrectedBy, unitCost: null,
                        avg, variantId, colorName);
                    break;
            }
        }

        return rows;
    }

    private static void Add(
        List<StockMovementDto> rows,
        DateTime at, StockMovementType type, decimal delta, decimal balance, string unit,
        string? reason, string? reference, string by, decimal? unitCost,
        decimal avg, Guid? variantId, string? colorName)
        => rows.Add(new StockMovementDto(
            at, type, delta, balance, unit, reason, reference, by, unitCost,
            RunningAverageCost: avg, BalanceValue: balance * avg, variantId, colorName));
}
