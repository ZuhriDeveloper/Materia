using FluentAssertions;
using Materia.Application.DTOs.Inventory;
using Materia.Application.Queries.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;
using Materia.Domain.Purchasing;

namespace Materia.Tests.Inventory;

public class StockMovementMapperTests
{
    private static readonly StockId Sid = StockId.New();
    private static readonly ProductId Pid = ProductId.New();
    private static readonly DateTime T0 = new(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Initialized_MapsToOpeningRow()
    {
        var events = new IDomainEvent[]
        {
            new StockInitialized(Sid, Pid, 0m, "sak", "user-1", T0),
        };

        var row = StockMovementMapper.Map(events).Single();

        row.Type.Should().Be(StockMovementType.Initial);
        row.Delta.Should().Be(0m);
        row.BalanceAfter.Should().Be(0m);
        row.Unit.Should().Be("sak");
        row.PerformedBy.Should().Be("user-1");
        row.RunningAverageCost.Should().Be(0m);
    }

    [Fact]
    public void PurchaseReceipt_SetsDeltaBalanceUnitCostAndAverage()
    {
        var poId = PurchaseOrderId.New();
        var events = new IDomainEvent[]
        {
            new StockInitialized(Sid, Pid, 0m, "sak", "user-1", T0),
            new StockReconciledFromPurchase(Sid, Pid, 10m, 10m, poId, 12_000m, "sak", "gudang", T0.AddHours(1)),
        };

        var receipt = StockMovementMapper.Map(events).Last();

        receipt.Type.Should().Be(StockMovementType.PurchaseReceipt);
        receipt.Delta.Should().Be(10m);
        receipt.BalanceAfter.Should().Be(10m);
        receipt.UnitCost.Should().Be(12_000m);
        receipt.RunningAverageCost.Should().Be(12_000m);
        receipt.BalanceValue.Should().Be(120_000m);
        receipt.Reference.Should().Be(poId.Value.ToString());
    }

    [Fact]
    public void TwoReceipts_BlendWeightedAverageAndValue()
    {
        var events = new IDomainEvent[]
        {
            new StockInitialized(Sid, Pid, 0m, "sak", "u", T0),
            new StockReconciledFromPurchase(Sid, Pid, 10m, 10m, PurchaseOrderId.New(), 12_000m, "sak", "g", T0.AddHours(1)),
            new StockReconciledFromPurchase(Sid, Pid, 10m, 20m, PurchaseOrderId.New(), 14_000m, "sak", "g", T0.AddHours(2)),
        };

        var last = StockMovementMapper.Map(events).Last();

        last.RunningAverageCost.Should().Be(13_000m);     // (120_000 + 140_000) / 20
        last.BalanceValue.Should().Be(260_000m);          // 20 * 13_000
    }

    [Fact]
    public void SaleAdjustment_IsClassifiedAsSale_AndKeepsAverage()
    {
        var events = new IDomainEvent[]
        {
            new StockInitialized(Sid, Pid, 0m, "sak", "u", T0),
            new StockReconciledFromPurchase(Sid, Pid, 10m, 10m, PurchaseOrderId.New(), 12_000m, "sak", "g", T0.AddHours(1)),
            new StockAdjusted(Sid, Pid, -3m, 7m, "Penjualan INV-2026-0007", "kasir", T0.AddHours(2)),
        };

        var sale = StockMovementMapper.Map(events).Last();

        sale.Type.Should().Be(StockMovementType.Sale);
        sale.Delta.Should().Be(-3m);
        sale.BalanceAfter.Should().Be(7m);
        sale.Reference.Should().Be("INV-2026-0007");
        sale.Reason.Should().BeNull();
        sale.RunningAverageCost.Should().Be(12_000m);     // issues don't change average
        sale.BalanceValue.Should().Be(84_000m);           // 7 * 12_000
    }

    [Fact]
    public void ManualAdjustment_IsClassifiedAsAdjustment_WithReason()
    {
        var events = new IDomainEvent[]
        {
            new StockInitialized(Sid, Pid, 0m, "sak", "u", T0),
            new StockAdjusted(Sid, Pid, 5m, 5m, "Stok awal", "admin", T0.AddHours(1)),
        };

        var adj = StockMovementMapper.Map(events).Last();

        adj.Type.Should().Be(StockMovementType.Adjustment);
        adj.Reason.Should().Be("Stok awal");
        adj.Reference.Should().BeNull();
    }

    [Fact]
    public void PurchaseReturn_LowersBalanceWithNegativeDelta()
    {
        var poId = PurchaseOrderId.New();
        var events = new IDomainEvent[]
        {
            new StockInitialized(Sid, Pid, 0m, "sak", "u", T0),
            new StockReconciledFromPurchase(Sid, Pid, 10m, 10m, poId, 12_000m, "sak", "g", T0.AddHours(1)),
            new StockReducedFromPurchaseReturn(Sid, Pid, 4m, 6m, poId, 12_000m, "sak", "g", T0.AddHours(2)),
        };

        var ret = StockMovementMapper.Map(events).Last();

        ret.Type.Should().Be(StockMovementType.PurchaseReturn);
        ret.Delta.Should().Be(-4m);
        ret.BalanceAfter.Should().Be(6m);
        ret.Reference.Should().Be(poId.Value.ToString());
    }

    [Fact]
    public void UnitCorrection_UpdatesUnit_WithoutChangingBalance()
    {
        var events = new IDomainEvent[]
        {
            new StockInitialized(Sid, Pid, 0m, "sak", "u", T0),
            new StockUnitCorrected(Sid, Pid, "zak", "admin", T0.AddHours(1)),
        };

        var rows = StockMovementMapper.Map(events);

        rows.Last().Type.Should().Be(StockMovementType.UnitCorrection);
        rows.Last().Delta.Should().Be(0m);
        rows.Last().Unit.Should().Be("zak");
    }

    [Fact]
    public void VariantContext_IsCarriedOntoEveryRow()
    {
        var variantId = Guid.NewGuid();
        var events = new IDomainEvent[]
        {
            new StockInitialized(Sid, Pid, 0m, "sak", "u", T0, variantId),
            new StockAdjusted(Sid, Pid, 2m, 2m, "Stok awal", "admin", T0.AddHours(1)),
        };

        var rows = StockMovementMapper.Map(events, variantId, "Merah");

        rows.Should().OnlyContain(r => r.VariantId == variantId && r.ColorName == "Merah");
    }
}
