using Materia.Domain.Common;

namespace Materia.Domain.Sales;

public record Money
{
    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("Jumlah tidak boleh negatif.");
        Amount = Math.Round(amount, 2);
    }

    public static Money Zero => new(0m);

    public Money Add(Money other)          => new(Amount + other.Amount);
    public Money Subtract(Money other)     => new(Amount - other.Amount);
    public Money Multiply(decimal factor)  => new(Amount * factor);

    public override string ToString() => Amount.ToString("N2");
}
