using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Sales;
using Materia.Domain.Sales.Events;

namespace Materia.Tests.Sales;

/// <summary>
/// Phase 1 line-pricing tests.
/// Covers: back-compat (no discount), line discount math, edge cases, cost/margin,
/// and replay of old-style SaleItemAdded events (without new fields).
/// </summary>
public class SaleLinePricingTests
{
    private const string Staff = "kasir-01";

    // ─────────────────────────────────────────────────────────────────────────
    // Back-compat: zero discount must produce identical totals to legacy path
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_NoDiscount_SubtotalAndGrandTotalMatchLegacy()
    {
        var sale = Sale.Create("INV-2001", Staff);
        sale.AddItem(Guid.NewGuid(), "Semen 50kg", "sak", 3m, 3m, 65_000m, Staff);

        // Subtotal = 3 × 65,000 = 195,000
        sale.Subtotal.Amount.Should().Be(195_000m);
        sale.GrossSubtotal.Amount.Should().Be(195_000m);
        sale.LineDiscountTotal.Amount.Should().Be(0m);

        sale.Finalize(Staff);
        sale.GrandTotal.Amount.Should().Be(195_000m);
    }

    [Fact]
    public void AddItem_NoDiscount_ItemNetUnitPriceEqualsListUnitPrice()
    {
        var sale = Sale.Create("INV-2002", Staff);
        sale.AddItem(Guid.NewGuid(), "Cat Tembok", "kaleng", 2m, 2m, 55_000m, Staff);

        var item = sale.Items.Single();
        item.ListUnitPrice.Amount.Should().Be(55_000m);
        item.DiscountPerUnit.Amount.Should().Be(0m);
        item.NetUnitPrice.Amount.Should().Be(55_000m);
        item.UnitPrice.Amount.Should().Be(55_000m);  // back-compat: UnitPrice = net price
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Line discount math
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_WithDiscountPerUnit_NetUnitPriceIsReduced()
    {
        var sale = Sale.Create("INV-2010", Staff);
        sale.AddItem(
            Guid.NewGuid(), "Semen 50kg", "sak", 4m, 4m,
            unitPrice: 65_000m, updatedBy: Staff,
            listUnitPrice: 70_000m, discountPerUnit: 5_000m);

        var item = sale.Items.Single();
        item.ListUnitPrice.Amount.Should().Be(70_000m);
        item.DiscountPerUnit.Amount.Should().Be(5_000m);
        item.NetUnitPrice.Amount.Should().Be(65_000m);      // 70,000 − 5,000
        item.UnitPrice.Amount.Should().Be(65_000m);         // back-compat getter = net
    }

    [Fact]
    public void AddItem_WithDiscountPerUnit_SubtotalUsesNetPrice()
    {
        var sale = Sale.Create("INV-2011", Staff);
        sale.AddItem(
            Guid.NewGuid(), "Semen 50kg", "sak", 4m, 4m,
            unitPrice: 65_000m, updatedBy: Staff,
            listUnitPrice: 70_000m, discountPerUnit: 5_000m);

        var item = sale.Items.Single();
        // Net subtotal = 65,000 × 4 = 260,000
        item.Subtotal.Amount.Should().Be(260_000m);
        // Gross subtotal = 70,000 × 4 = 280,000
        item.GrossSubtotal.Amount.Should().Be(280_000m);
        // Line discount = 280,000 − 260,000 = 20,000
        item.LineDiscount.Amount.Should().Be(20_000m);
    }

    [Fact]
    public void AddItem_WithDiscount_SaleSubtotalAndGrossSubtotalCorrect()
    {
        var sale = Sale.Create("INV-2012", Staff);
        sale.AddItem(
            Guid.NewGuid(), "Semen", "sak", 4m, 4m,
            unitPrice: 65_000m, updatedBy: Staff,
            listUnitPrice: 70_000m, discountPerUnit: 5_000m);
        sale.AddItem(
            Guid.NewGuid(), "Pasir", "sak", 2m, 2m,
            unitPrice: 30_000m, updatedBy: Staff);  // no discount

        // Sale.Subtotal = net = 4×65,000 + 2×30,000 = 260,000 + 60,000 = 320,000
        sale.Subtotal.Amount.Should().Be(320_000m);
        // Sale.GrossSubtotal = gross = 4×70,000 + 2×30,000 = 280,000 + 60,000 = 340,000
        sale.GrossSubtotal.Amount.Should().Be(340_000m);
        // LineDiscountTotal = 20,000 + 0 = 20,000
        sale.LineDiscountTotal.Amount.Should().Be(20_000m);
    }

    [Fact]
    public void AddItem_WithDiscount_GrandTotalUsesNetSubtotal()
    {
        var sale = Sale.Create("INV-2013", Staff);
        sale.AddItem(
            Guid.NewGuid(), "Semen", "sak", 2m, 2m,
            unitPrice: 90_000m, updatedBy: Staff,
            listUnitPrice: 100_000m, discountPerUnit: 10_000m);

        // Net subtotal = 2 × 90,000 = 180,000
        sale.Finalize(Staff);
        sale.GrandTotal.Amount.Should().Be(180_000m);  // no order discount, no tax
    }

    [Fact]
    public void AddItem_DiscountEqualToListPrice_NetUnitPriceIsZero()
    {
        var sale = Sale.Create("INV-2014", Staff);
        sale.AddItem(
            Guid.NewGuid(), "Promo Item", "pcs", 1m, 1m,
            unitPrice: 0m, updatedBy: Staff,
            listUnitPrice: 50_000m, discountPerUnit: 50_000m);

        var item = sale.Items.Single();
        item.NetUnitPrice.Amount.Should().Be(0m);
        item.LineDiscount.Amount.Should().Be(50_000m);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Guard: discount > list price throws
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_DiscountExceedsListPrice_ThrowsDomainException()
    {
        var sale = Sale.Create("INV-2020", Staff);

        Action act = () => sale.AddItem(
            Guid.NewGuid(), "Item", "pcs", 1m, 1m,
            unitPrice: 0m, updatedBy: Staff,
            listUnitPrice: 50_000m, discountPerUnit: 60_000m);  // discount > list!

        act.Should().Throw<DomainException>().WithMessage("*Diskon*");
    }

    [Fact]
    public void AddItem_NegativeDiscount_ThrowsDomainException()
    {
        var sale = Sale.Create("INV-2021", Staff);

        Action act = () => sale.AddItem(
            Guid.NewGuid(), "Item", "pcs", 1m, 1m,
            unitPrice: 65_000m, updatedBy: Staff,
            discountPerUnit: -1_000m);

        act.Should().Throw<DomainException>().WithMessage("*Diskon*");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cost and margin
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_WithUnitCost_LineCostUsesQuantityInBaseUnit()
    {
        var sale = Sale.Create("INV-2030", Staff);
        // 2 sak = 2 base units; cost per base unit = 40,000
        sale.AddItem(
            Guid.NewGuid(), "Semen", "sak", 2m, 2m,
            unitPrice: 65_000m, updatedBy: Staff,
            unitCost: 40_000m);

        var item = sale.Items.Single();
        item.UnitCost.Amount.Should().Be(40_000m);
        // LineCost = UnitCost × QuantityInBaseUnit = 40,000 × 2 = 80,000
        item.LineCost.Amount.Should().Be(80_000m);
    }

    [Fact]
    public void AddItem_WithUnitCost_LineMarginIsNetSubtotalMinusCost()
    {
        var sale = Sale.Create("INV-2031", Staff);
        // Net = 65,000 × 2 = 130,000; cost = 40,000 × 2 = 80,000
        sale.AddItem(
            Guid.NewGuid(), "Semen", "sak", 2m, 2m,
            unitPrice: 65_000m, updatedBy: Staff,
            unitCost: 40_000m);

        var item = sale.Items.Single();
        item.LineMargin.Should().Be(50_000m);  // 130,000 − 80,000
    }

    [Fact]
    public void Sale_TotalCostAndGrossMargin_AreAggregatesOfLines()
    {
        var sale = Sale.Create("INV-2032", Staff);
        sale.AddItem(
            Guid.NewGuid(), "Semen", "sak", 2m, 2m,
            unitPrice: 65_000m, updatedBy: Staff,
            unitCost: 40_000m);
        sale.AddItem(
            Guid.NewGuid(), "Pasir", "sak", 3m, 3m,
            unitPrice: 30_000m, updatedBy: Staff,
            unitCost: 20_000m);

        // TotalCost = 40,000×2 + 20,000×3 = 80,000 + 60,000 = 140,000
        sale.TotalCost.Amount.Should().Be(140_000m);
        // GrossMargin = (65k×2 + 30k×3) − 140,000 = (130k+90k) − 140k = 80,000
        sale.GrossMargin.Should().Be(80_000m);
    }

    [Fact]
    public void AddItem_NoCostProvided_UnitCostIsZeroAndLineCostIsZero()
    {
        var sale = Sale.Create("INV-2033", Staff);
        sale.AddItem(Guid.NewGuid(), "Semen", "sak", 2m, 2m, 65_000m, Staff);

        var item = sale.Items.Single();
        item.UnitCost.Amount.Should().Be(0m);
        item.LineCost.Amount.Should().Be(0m);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GrossSubtotal / LineDiscountTotal on Sale aggregate
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sale_GrossSubtotal_IsZeroBeforeItemsAdded()
    {
        var sale = Sale.Create("INV-2040", Staff);
        sale.GrossSubtotal.Amount.Should().Be(0m);
        sale.LineDiscountTotal.Amount.Should().Be(0m);
        sale.TotalCost.Amount.Should().Be(0m);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Back-compat replay: old SaleItemAdded without new fields
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_OldStyleSaleItemAdded_DefaultsNewFieldsGracefully()
    {
        // Simulate an old event with only the original fields (no new optional params)
        var saleId   = SaleId.New();
        var itemId   = SaleItemId.New();
        var productId = Guid.NewGuid();

        var oldEvent = new SaleItemAdded(
            saleId, itemId,
            productId, "Semen 50kg",
            "sak", 3m, 3m, 65_000m,
            Staff, DateTime.UtcNow
            // No VariantId, ColorName, ListUnitPrice, DiscountPerUnit, UnitCost, etc.
        );

        var saleCreated = new SaleCreated(saleId, "INV-OLD-001", Staff, DateTime.UtcNow);
        var sale = Sale.Reconstitute([saleCreated, oldEvent]);

        var item = sale.Items.Single();
        // ListUnitPrice should default to UnitPrice
        item.ListUnitPrice.Amount.Should().Be(65_000m);
        // DiscountPerUnit defaults to 0
        item.DiscountPerUnit.Amount.Should().Be(0m);
        // NetUnitPrice = ListUnitPrice − DiscountPerUnit = 65,000
        item.NetUnitPrice.Amount.Should().Be(65_000m);
        // UnitCost defaults to 0
        item.UnitCost.Amount.Should().Be(0m);
        // Subtotal unchanged: 3 × 65,000 = 195,000
        item.Subtotal.Amount.Should().Be(195_000m);
        // GrandTotal unchanged from legacy
        sale.GrandTotal.Amount.Should().Be(195_000m);
    }

    [Fact]
    public void Reconstitute_NewSaleItemAdded_RestoresAllFields()
    {
        var saleId    = SaleId.New();
        var itemId    = SaleItemId.New();
        var productId = Guid.NewGuid();

        var createdEvt = new SaleCreated(saleId, "INV-NEW-001", Staff, DateTime.UtcNow);
        var addedEvt = new SaleItemAdded(
            saleId, itemId,
            productId, "Cat Tembok",
            "kaleng", 5m, 5m, 45_000m,
            Staff, DateTime.UtcNow,
            VariantId: null, ColorName: null,
            ListUnitPrice: 50_000m,
            DiscountPerUnit: 5_000m,
            UnitCost: 30_000m);

        var sale = Sale.Reconstitute([createdEvt, addedEvt]);

        var item = sale.Items.Single();
        item.ListUnitPrice.Amount.Should().Be(50_000m);
        item.DiscountPerUnit.Amount.Should().Be(5_000m);
        item.NetUnitPrice.Amount.Should().Be(45_000m);
        item.UnitCost.Amount.Should().Be(30_000m);
        item.LineCost.Amount.Should().Be(150_000m);   // 30,000 × 5
        item.Subtotal.Amount.Should().Be(225_000m);   // 45,000 × 5
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SaleItemAdded event carries new fields
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_WithDiscount_EventContainsListUnitPriceAndDiscountPerUnit()
    {
        var sale = Sale.Create("INV-2050", Staff);
        sale.AddItem(
            Guid.NewGuid(), "Semen", "sak", 2m, 2m,
            unitPrice: 60_000m, updatedBy: Staff,
            listUnitPrice: 70_000m, discountPerUnit: 10_000m,
            unitCost: 42_000m);

        var evt = sale.DomainEvents.OfType<SaleItemAdded>().Single();
        evt.ListUnitPrice.Should().Be(70_000m);
        evt.DiscountPerUnit.Should().Be(10_000m);
        evt.UnitCost.Should().Be(42_000m);
    }

    [Fact]
    public void AddItem_NoListPrice_EventListUnitPriceDefaultsToUnitPrice()
    {
        var sale = Sale.Create("INV-2051", Staff);
        sale.AddItem(Guid.NewGuid(), "Semen", "sak", 1m, 1m, 65_000m, Staff);

        var evt = sale.DomainEvents.OfType<SaleItemAdded>().Single();
        evt.ListUnitPrice.Should().Be(65_000m);  // defaulted to UnitPrice
        evt.DiscountPerUnit.Should().Be(0m);
        evt.UnitCost.Should().Be(0m);
    }
}
