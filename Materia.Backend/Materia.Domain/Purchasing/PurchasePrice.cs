using Materia.Domain.Common;

namespace Materia.Domain.Purchasing;

public record PurchasePrice
{
    public decimal Amount { get; }
    public string Currency { get; }
    public string Unit { get; }
    public DateTime EffectiveFrom { get; }

    /// <summary>
    /// Standing chained trade discount on this list price (% per level, e.g. [12.5, 7, 5]),
    /// or empty when the supplier grants no discount on this product. Pre-fills the PO line.
    /// </summary>
    public IReadOnlyList<decimal> Discounts { get; }

    public PurchasePrice(
        decimal amount, string currency, string unit, DateTime effectiveFrom,
        IReadOnlyList<decimal>? discounts = null)
    {
        if (amount <= 0) throw new DomainException("Purchase price must be positive.");
        if (string.IsNullOrWhiteSpace(currency)) throw new DomainException("Currency is required.");
        if (string.IsNullOrWhiteSpace(unit)) throw new DomainException("Unit is required.");

        Discounts = discounts ?? [];
        if (Discounts.Count > 0)
            DiscountChain.Validate(Discounts);

        Amount = amount;
        Currency = currency;
        Unit = unit;
        EffectiveFrom = effectiveFrom;
    }

    /// <summary>Net buy price after folding the standing discount chain into the list amount.</summary>
    public decimal NetAmount => DiscountChain.ComputeNet(Amount, Discounts);
}
