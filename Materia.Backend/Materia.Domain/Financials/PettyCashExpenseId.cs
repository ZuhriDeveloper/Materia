namespace Materia.Domain.Financials;

public record PettyCashExpenseId(Guid Value)
{
    public static PettyCashExpenseId New() => new(Guid.NewGuid());
    public static PettyCashExpenseId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
