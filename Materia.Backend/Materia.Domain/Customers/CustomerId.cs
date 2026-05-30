namespace Materia.Domain.Customers;

public record CustomerId(Guid Value)
{
    public static CustomerId New()         => new(Guid.NewGuid());
    public static CustomerId From(Guid id) => new(id);
    public override string ToString()      => Value.ToString();
}
