using Materia.Domain.Common;

namespace Materia.Domain.Purchasing;

public record PurchasePrice
{
    public decimal Amount { get; }
    public string Currency { get; }
    public string Unit { get; }
    public DateTime EffectiveFrom { get; }

    public PurchasePrice(decimal amount, string currency, string unit, DateTime effectiveFrom)
    {
        if (amount <= 0) throw new DomainException("Purchase price must be positive.");
        if (string.IsNullOrWhiteSpace(currency)) throw new DomainException("Currency is required.");
        if (string.IsNullOrWhiteSpace(unit)) throw new DomainException("Unit is required.");

        Amount = amount;
        Currency = currency;
        Unit = unit;
        EffectiveFrom = effectiveFrom;
    }
}
