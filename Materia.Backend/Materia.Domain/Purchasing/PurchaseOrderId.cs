namespace Materia.Domain.Purchasing;

public record PurchaseOrderId(Guid Value)
{
    public static PurchaseOrderId New() => new(Guid.NewGuid());
    public static PurchaseOrderId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
