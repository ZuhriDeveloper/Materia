namespace Materia.Domain.Inventory;

public record UnitId(Guid Value)
{
    public static UnitId New() => new(Guid.NewGuid());
    public static UnitId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
