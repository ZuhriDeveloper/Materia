namespace Materia.Domain.Sales;

public record SaleId(Guid Value)
{
    public static SaleId New()         => new(Guid.NewGuid());
    public static SaleId From(Guid id) => new(id);
    public override string ToString()  => Value.ToString();
}
