namespace Materia.Domain.Purchasing;

public record SupplierId(Guid Value)
{
    public static SupplierId New() => new(Guid.NewGuid());
    public static SupplierId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
