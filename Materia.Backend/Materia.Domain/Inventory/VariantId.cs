namespace Materia.Domain.Inventory;

public record VariantId(Guid Value)
{
    public static VariantId New() => new(Guid.NewGuid());
    public static VariantId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
