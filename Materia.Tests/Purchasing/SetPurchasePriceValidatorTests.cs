using FluentAssertions;
using Materia.Application.Commands.Purchasing.SetPurchasePrice;

namespace Materia.Tests.Purchasing;

public class SetPurchasePriceValidatorTests
{
    private readonly SetPurchasePriceCommandValidator _validator = new();

    private static SetPurchasePriceCommand Valid(IReadOnlyList<decimal>? discounts = null) =>
        new(
            SupplierId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Amount: 120_000m,
            Currency: "IDR",
            Unit: "batang",
            EffectiveFrom: DateTime.UtcNow,
            UpdatedBy: "admin",
            Discounts: discounts);

    [Fact]
    public void NoDiscount_Passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidDiscountChain_Passes()
    {
        _validator.Validate(Valid([12.5m, 7m, 5m])).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DiscountChainOverSixLevels_Fails()
    {
        var result = _validator.Validate(Valid([1m, 2m, 3m, 4m, 5m, 6m, 7m]));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(-5)]
    public void DiscountOutOfRange_Fails(decimal pct)
    {
        var result = _validator.Validate(Valid([pct]));
        result.IsValid.Should().BeFalse();
    }
}
