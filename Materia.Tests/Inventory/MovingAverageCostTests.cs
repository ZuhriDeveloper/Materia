using FluentAssertions;
using Materia.Application.Services;

namespace Materia.Tests.Inventory;

public class MovingAverageCostTests
{
    [Fact]
    public void FirstReceipt_TakesUnitCost()
    {
        MovingAverageCost.AfterReceipt(prevQty: 0m, prevAvg: 0m, receivedQty: 10m, unitCost: 12_000m)
            .Should().Be(12_000m);
    }

    [Fact]
    public void SecondReceipt_BlendsWeightedAverage()
    {
        // 10 @ 12_000 then 10 @ 14_000 → (120_000 + 140_000) / 20 = 13_000
        var avg = MovingAverageCost.AfterReceipt(0m, 0m, 10m, 12_000m);
        MovingAverageCost.AfterReceipt(10m, avg, 10m, 14_000m).Should().Be(13_000m);
    }

    [Fact]
    public void UnequalQuantities_AreWeightedByQuantity()
    {
        // 30 @ 10 then 10 @ 20 → (300 + 200) / 40 = 12.5
        MovingAverageCost.AfterReceipt(30m, 10m, 10m, 20m).Should().Be(12.5m);
    }

    [Fact]
    public void NonPositivePriorQuantity_ResetsToUnitCost()
    {
        // Stock had been driven to zero/negative; the next receipt re-bases the average.
        MovingAverageCost.AfterReceipt(prevQty: -3m, prevAvg: 9_999m, receivedQty: 5m, unitCost: 8_000m)
            .Should().Be(8_000m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void NonPositiveReceipt_LeavesAverageUnchanged(decimal receivedQty)
    {
        MovingAverageCost.AfterReceipt(10m, 5_000m, receivedQty, 9_000m).Should().Be(5_000m);
    }
}
