namespace Materia.Domain.Financials;

public record ChangeFundDepositId(Guid Value)
{
    public static ChangeFundDepositId New()            => new(Guid.NewGuid());
    public static ChangeFundDepositId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
