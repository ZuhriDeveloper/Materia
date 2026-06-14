using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Purchasing;

namespace Materia.Tests.Purchasing;

public class DiscountChainTests
{
    [Fact]
    public void ComputeNet_ChainedDiscounts_FoldsMultiplicatively()
    {
        // 100000 × 0.875 × 0.93 × 0.95 = 77306.25
        DiscountChain.ComputeNet(100_000m, [12.5m, 7m, 5m]).Should().Be(77_306.25m);
    }

    [Fact]
    public void ComputeNet_SingleDiscount_AppliesOnce()
    {
        DiscountChain.ComputeNet(200_000m, [10m]).Should().Be(180_000m);
    }

    [Fact]
    public void ComputeNet_EmptyOrNull_ReturnsListUnchanged()
    {
        DiscountChain.ComputeNet(50_000m, []).Should().Be(50_000m);
        DiscountChain.ComputeNet(50_000m, null).Should().Be(50_000m);
    }

    [Fact]
    public void ComputeNet_RoundsToTwoDecimals()
    {
        // 12345 × 0.965 = 11912.925 → 11912.93 (away from zero)
        DiscountChain.ComputeNet(12_345m, [3.5m]).Should().Be(11_912.93m);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(-1)]
    public void Validate_OutOfRangeDiscount_Throws(decimal bad)
    {
        Action act = () => DiscountChain.Validate([5m, bad]);
        act.Should().Throw<DomainException>().WithMessage("*between 0 and 100*");
    }

    [Fact]
    public void Validate_TooManyLevels_Throws()
    {
        Action act = () => DiscountChain.Validate([1m, 2m, 3m, 4m, 5m, 6m, 7m]);
        act.Should().Throw<DomainException>().WithMessage("*at most*");
    }
}
