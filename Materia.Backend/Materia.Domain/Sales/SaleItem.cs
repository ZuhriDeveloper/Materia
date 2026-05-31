using Materia.Domain.Common;

namespace Materia.Domain.Sales;

public sealed class SaleItem
{
    public SaleItemId Id                { get; }
    public Guid       ProductId         { get; }
    public string     ProductName       { get; }
    public string     UnitName          { get; }   // unit as entered (e.g. "sak")
    public decimal    Quantity          { get; }   // quantity in entered unit
    public decimal    QuantityInBaseUnit { get; }  // converted to product base unit
    public Money      UnitPrice         { get; }   // price per entered unit
    public Money      Subtotal          => UnitPrice.Multiply(Quantity);

    internal SaleItem(
        SaleItemId id,
        Guid       productId,
        string     productName,
        string     unitName,
        decimal    quantity,
        decimal    quantityInBaseUnit,
        Money      unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("Kuantitas harus lebih dari nol.");
        if (quantityInBaseUnit <= 0)
            throw new DomainException("Kuantitas satuan dasar harus lebih dari nol.");

        Id                 = id;
        ProductId          = productId;
        ProductName        = productName;
        UnitName           = unitName;
        Quantity           = quantity;
        QuantityInBaseUnit = quantityInBaseUnit;
        UnitPrice          = unitPrice;
    }
}
