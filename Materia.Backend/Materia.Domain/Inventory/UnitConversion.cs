using Materia.Domain.Common;

namespace Materia.Domain.Inventory;

/// <summary>
/// Describes how many <see cref="ToUnit"/> items make up one <see cref="FromUnit"/>.
/// Example: FromUnit="sak", ToUnit="bungkus", Factor=5 → 1 sak = 5 bungkus.
/// </summary>
public record UnitConversion
{
    public UnitName FromUnit { get; }
    public UnitName ToUnit { get; }
    public decimal Factor { get; }

    /// <summary>Sale price when selling one <see cref="ToUnit"/>.</summary>
    public decimal SalePrice { get; }

    public UnitConversion(UnitName fromUnit, UnitName toUnit, decimal factor, decimal salePrice = 0m)
    {
        if (factor <= 0)
            throw new DomainException("Conversion factor must be greater than zero.");
        if (fromUnit == toUnit)
            throw new DomainException("FromUnit and ToUnit must be different.");
        if (salePrice < 0)
            throw new DomainException("Sale price cannot be negative.");

        FromUnit = fromUnit;
        ToUnit = toUnit;
        Factor = factor;
        SalePrice = salePrice;
    }

    /// <summary>Returns the equivalent quantity in <see cref="ToUnit"/>.</summary>
    public decimal Convert(decimal quantityInFromUnit) => quantityInFromUnit * Factor;
}
