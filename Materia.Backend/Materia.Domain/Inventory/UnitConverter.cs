namespace Materia.Domain.Inventory;

public static class UnitConverter
{
    /// <summary>
    /// Converts <paramref name="quantity"/> from <paramref name="fromUnit"/> to <paramref name="toUnit"/>
    /// using the product's unit conversions. Supports direct and reverse conversions.
    /// Returns false if no conversion path is found.
    /// </summary>
    public static bool TryConvert(
        decimal quantity,
        UnitName fromUnit,
        UnitName toUnit,
        IReadOnlyList<UnitConversion> conversions,
        out decimal result)
    {
        if (fromUnit == toUnit)
        {
            result = quantity;
            return true;
        }

        // Direct: fromUnit → toUnit
        var direct = conversions.FirstOrDefault(c => c.FromUnit == fromUnit && c.ToUnit == toUnit);
        if (direct is not null)
        {
            result = direct.Convert(quantity);
            return true;
        }

        // Reverse: toUnit → fromUnit stored, so invert
        var reverse = conversions.FirstOrDefault(c => c.FromUnit == toUnit && c.ToUnit == fromUnit);
        if (reverse is not null)
        {
            result = quantity / reverse.Factor;
            return true;
        }

        result = 0;
        return false;
    }
}
