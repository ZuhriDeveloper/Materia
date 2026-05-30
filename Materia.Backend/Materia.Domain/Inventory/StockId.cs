namespace Materia.Domain.Inventory;

public record StockId(Guid Value)
{
    public static StockId New() => new(Guid.NewGuid());
    public static StockId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
