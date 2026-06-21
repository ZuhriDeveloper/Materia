namespace Materia.Domain.Sales;

public readonly record struct SaleReturnId(Guid Value)
{
    public static SaleReturnId New()          => new(Guid.NewGuid());
    public static SaleReturnId From(Guid value) => new(value);
    public override string ToString()          => Value.ToString();
}
