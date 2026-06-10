using FluentAssertions;
using Materia.Application.Commands.Sales.FinalizeSale;

namespace Materia.Tests.Sales;

/// <summary>
/// Validator rules for the new line-pricing fields in FinalizeSaleItemInput.
/// </summary>
public class FinalizeSaleLinePricingValidatorTests
{
    private readonly FinalizeSaleCommandValidator _validator = new();

    private static FinalizeSaleCommand ValidCommand(
        decimal unitPrice      = 65_000m,
        decimal? listUnitPrice = null,
        decimal? discountPerUnit = null) =>
        new(
            CustomerId:         null,
            CustomerName:       null,
            Items:              [new FinalizeSaleItemInput(
                                    Guid.NewGuid(), "Semen 50kg", "sak", 2m, unitPrice,
                                    ListUnitPrice:   listUnitPrice,
                                    DiscountPerUnit: discountPerUnit)],
            IsDeliveryRequired: false,
            ServedBy:           "kasir-01");

    [Fact]
    public void ValidCommand_NoDiscount_Passes()
    {
        var result = _validator.Validate(ValidCommand(unitPrice: 65_000m));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidCommand_WithValidDiscount_Passes()
    {
        // listUnitPrice=70k, unitPrice=60k (=net), discountPerUnit=10k
        var result = _validator.Validate(ValidCommand(60_000m, 70_000m, 10_000m));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DiscountPerUnit_Negative_Fails()
    {
        var result = _validator.Validate(ValidCommand(65_000m, null, -1_000m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains(nameof(FinalizeSaleItemInput.DiscountPerUnit)));
    }

    [Fact]
    public void ListUnitPrice_LessThanUnitPrice_Fails()
    {
        // unitPrice = 65,000 but listUnitPrice = 50,000 — list must be >= unitPrice
        var result = _validator.Validate(ValidCommand(65_000m, listUnitPrice: 50_000m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains(nameof(FinalizeSaleItemInput.ListUnitPrice)));
    }

    [Fact]
    public void DiscountPerUnit_ExceedsListUnitPrice_Fails()
    {
        // listUnitPrice=70k; discountPerUnit=80k — discount > list
        var result = _validator.Validate(ValidCommand(0m, 70_000m, 80_000m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains(nameof(FinalizeSaleItemInput.DiscountPerUnit)));
    }

    [Fact]
    public void Quantity_Zero_StillFails()
    {
        var cmd = new FinalizeSaleCommand(
            null, null,
            [new FinalizeSaleItemInput(Guid.NewGuid(), "X", "pcs", 0m, 10_000m)],
            false, "kasir-01");

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // H1 — net-price consistency rules
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// (UnitPrice=60000, ListUnitPrice=70000, DiscountPerUnit=30000): net would be
    /// 70000-30000=40000 but UnitPrice says 60000 — inconsistent, must be rejected.
    /// </summary>
    [Fact]
    public void InconsistentTriple_UnitPriceNotEqualToNetPrice_Fails()
    {
        // net = 70_000 - 30_000 = 40_000, but UnitPrice = 60_000 → mismatch
        var result = _validator.Validate(ValidCommand(60_000m, 70_000m, 30_000m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains(nameof(FinalizeSaleItemInput.UnitPrice)));
    }

    /// <summary>
    /// (UnitPrice=40000, ListUnitPrice=70000, DiscountPerUnit=30000): net = 70000-30000 = 40000
    /// — consistent triple must be accepted.
    /// </summary>
    [Fact]
    public void ConsistentTriple_UnitPriceEqualsNetPrice_Passes()
    {
        // net = 70_000 - 30_000 = 40_000; UnitPrice = 40_000 → consistent
        var result = _validator.Validate(ValidCommand(40_000m, 70_000m, 30_000m));
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Back-compat: item with only UnitPrice (no ListUnitPrice, no DiscountPerUnit) must pass.
    /// </summary>
    [Fact]
    public void BackCompat_OnlyUnitPrice_NoListOrDiscount_Passes()
    {
        var result = _validator.Validate(ValidCommand(unitPrice: 75_000m));
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Zero-discount line where UnitPrice == ListUnitPrice and DiscountPerUnit == 0 must pass.
    /// </summary>
    [Fact]
    public void ZeroDiscountLine_UnitPriceEqualsListUnitPrice_Passes()
    {
        // UnitPrice = ListUnitPrice = 65_000, DiscountPerUnit = 0 → net = 65_000 - 0 = 65_000
        var result = _validator.Validate(ValidCommand(65_000m, 65_000m, 0m));
        result.IsValid.Should().BeTrue();
    }
}
