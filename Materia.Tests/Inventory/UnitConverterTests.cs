using FluentAssertions;
using Materia.Domain.Inventory;

namespace Materia.Tests.Inventory;

public class UnitConverterTests
{
    private static readonly UnitName Sak = new("sak");
    private static readonly UnitName Bungkus = new("bungkus");
    private static readonly UnitName Kg = new("kg");

    private static readonly List<UnitConversion> Conversions =
    [
        new UnitConversion(Sak, Bungkus, 5),   // 1 sak = 5 bungkus
        new UnitConversion(Sak, Kg, 50),        // 1 sak = 50 kg
    ];

    [Fact]
    public void Convert_SameUnit_ReturnsSameQuantity()
    {
        UnitConverter.TryConvert(10m, Sak, Sak, Conversions, out var result).Should().BeTrue();
        result.Should().Be(10m);
    }

    [Fact]
    public void Convert_DirectConversion_ReturnsCorrectValue()
    {
        UnitConverter.TryConvert(2m, Sak, Bungkus, Conversions, out var result).Should().BeTrue();
        result.Should().Be(10m); // 2 sak * 5 = 10 bungkus
    }

    [Fact]
    public void Convert_ReverseConversion_ReturnsCorrectValue()
    {
        UnitConverter.TryConvert(10m, Bungkus, Sak, Conversions, out var result).Should().BeTrue();
        result.Should().Be(2m); // 10 bungkus / 5 = 2 sak
    }

    [Fact]
    public void Convert_UnknownUnit_ReturnsFalse()
    {
        var unknown = new UnitName("meter");
        UnitConverter.TryConvert(1m, Sak, unknown, Conversions, out _).Should().BeFalse();
    }

    [Fact]
    public void Convert_EmptyConversions_ReturnsFalse()
    {
        UnitConverter.TryConvert(1m, Sak, Bungkus, [], out _).Should().BeFalse();
    }
}
