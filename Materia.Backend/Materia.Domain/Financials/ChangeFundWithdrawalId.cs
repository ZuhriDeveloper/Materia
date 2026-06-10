namespace Materia.Domain.Financials;

public record ChangeFundWithdrawalId(Guid Value)
{
    public static ChangeFundWithdrawalId New()            => new(Guid.NewGuid());
    public static ChangeFundWithdrawalId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
