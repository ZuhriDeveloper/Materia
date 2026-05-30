namespace Materia.Domain.Customers;

public record AddressId(Guid Value)
{
    public static AddressId New()         => new(Guid.NewGuid());
    public static AddressId From(Guid id) => new(id);
    public override string ToString()     => Value.ToString();
}
