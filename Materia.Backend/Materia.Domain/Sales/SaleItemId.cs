namespace Materia.Domain.Sales;

public record SaleItemId(Guid Value)
{
    public static SaleItemId New()         => new(Guid.NewGuid());
    public static SaleItemId From(Guid id) => new(id);
    public override string ToString()      => Value.ToString();
}
