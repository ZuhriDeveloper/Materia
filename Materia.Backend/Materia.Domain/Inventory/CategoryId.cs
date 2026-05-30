namespace Materia.Domain.Inventory;

public record CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.NewGuid());
    public static CategoryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
