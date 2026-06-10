namespace Materia.Domain.Stores;

public record StoreId(Guid Value)
{
    public static StoreId New() => new(Guid.NewGuid());
    public static StoreId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
