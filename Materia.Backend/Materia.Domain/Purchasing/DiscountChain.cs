using Materia.Domain.Common;

namespace Materia.Domain.Purchasing;

/// <summary>
/// Multiplicative chained trade discount on a buy price (e.g. 12,5%+7%+5%):
/// net = list × (1 − d1/100) × (1 − d2/100) × … Each level applies to the
/// running balance, not to the original list price.
/// </summary>
public static class DiscountChain
{
    private const int MaxLevels = 6;

    /// <summary>Net price after applying the discount chain to <paramref name="listAmount"/>.</summary>
    public static decimal ComputeNet(decimal listAmount, IReadOnlyList<decimal>? discounts)
    {
        if (discounts is null || discounts.Count == 0)
            return listAmount;

        Validate(discounts);

        var net = discounts.Aggregate(listAmount, (running, d) => running * (1m - d / 100m));
        return Math.Round(net, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Validates each discount is in the range [0, 100) and the chain isn't excessively long.</summary>
    public static void Validate(IReadOnlyList<decimal> discounts)
    {
        if (discounts.Count > MaxLevels)
            throw new DomainException($"A discount chain may have at most {MaxLevels} levels.");

        foreach (var d in discounts)
        {
            if (d < 0m || d >= 100m)
                throw new DomainException($"Each discount must be between 0 and 100 percent. Got: {d}.");
        }
    }
}
