using FluentAssertions;
using Materia.Domain.Customers;

namespace Materia.Tests.Customers;

public class DistanceCalculatorTests
{
    // Jakarta → Surabaya ≈ 665 km (known value)
    private static readonly Coordinates Jakarta   = new(-6.200000m,  106.816666m);
    private static readonly Coordinates Surabaya  = new(-7.257472m,  112.752090m);

    [Fact]
    public void BetweenKm_SamePoint_ReturnsZero()
    {
        DistanceCalculator.BetweenKm(Jakarta, Jakarta).Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void BetweenKm_JakartaToSurabaya_IsApproximately665Km()
    {
        var dist = DistanceCalculator.BetweenKm(Jakarta, Surabaya);
        dist.Should().BeApproximately(665, 20); // ±20 km tolerance
    }

    [Fact]
    public void BetweenKm_IsSymmetric()
    {
        var ab = DistanceCalculator.BetweenKm(Jakarta, Surabaya);
        var ba = DistanceCalculator.BetweenKm(Surabaya, Jakarta);
        ab.Should().BeApproximately(ba, 0.001);
    }

    [Fact]
    public void GetDeliveryTier_Within5Km_ReturnsLocal()
    {
        // Same city — very close points
        var a = new Coordinates(-6.200000m, 106.816666m);
        var b = new Coordinates(-6.201000m, 106.817000m);  // ~150m apart
        DistanceCalculator.GetDeliveryTier(a, b).Should().Be(DeliveryTier.Local);
    }

    [Fact]
    public void GetDeliveryTier_JakartaToSurabaya_ReturnsLongDistance()
    {
        // ~665 km, exceeds 100 km threshold for Regional
        DistanceCalculator.GetDeliveryTier(Jakarta, Surabaya).Should().Be(DeliveryTier.LongDistance);
    }
}
