using Materia.Domain.Common;

namespace Materia.Domain.Purchasing;

public enum PaymentTermUnit { Days, Weeks, Months }

/// <summary>
/// Term of payment (tempo) for a purchase order — e.g. 1 week, 2 months.
/// A <c>null</c> <see cref="PaymentTerm"/> on a PO means cash / no tempo (due on receipt).
/// </summary>
public sealed record PaymentTerm
{
    public int Value { get; }
    public PaymentTermUnit Unit { get; }

    public PaymentTerm(int value, PaymentTermUnit unit)
    {
        if (value <= 0)
            throw new DomainException("Payment term must be a positive period.");

        Value = value;
        Unit = unit;
    }

    /// <summary>The due date (jatuh tempo) for this term measured from <paramref name="anchor"/>.</summary>
    public DateTime DueDateFrom(DateTime anchor) => Unit switch
    {
        PaymentTermUnit.Days   => anchor.AddDays(Value),
        PaymentTermUnit.Weeks  => anchor.AddDays(Value * 7),
        PaymentTermUnit.Months => anchor.AddMonths(Value),
        _ => throw new DomainException($"Unknown payment term unit: {Unit}."),
    };

    /// <summary>Builds a term from raw stored values, or <c>null</c> when no tempo was set (cash).</summary>
    public static PaymentTerm? FromRaw(int? value, string? unit)
    {
        if (value is not { } v || string.IsNullOrWhiteSpace(unit))
            return null;
        if (!Enum.TryParse<PaymentTermUnit>(unit, ignoreCase: true, out var parsedUnit))
            throw new DomainException($"Unknown payment term unit: '{unit}'.");

        return new PaymentTerm(v, parsedUnit);
    }
}
