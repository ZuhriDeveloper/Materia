namespace Materia.Domain.Sales;

public sealed class SaleReturnLine
{
    public Guid    ProductId          { get; }
    public string  ProductName        { get; }
    public Guid?   VariantId          { get; }
    public string? ColorName          { get; }
    public string  UnitName           { get; }
    public decimal Quantity           { get; }
    public decimal QuantityInBaseUnit { get; }
    public decimal UnitPrice          { get; }
    public decimal Subtotal           => Quantity * UnitPrice;

    internal SaleReturnLine(
        Guid productId, string productName, Guid? variantId, string? colorName,
        string unitName, decimal quantity, decimal quantityInBaseUnit, decimal unitPrice)
    {
        ProductId          = productId;
        ProductName        = productName;
        VariantId          = variantId;
        ColorName          = colorName;
        UnitName           = unitName;
        Quantity           = quantity;
        QuantityInBaseUnit = quantityInBaseUnit;
        UnitPrice          = unitPrice;
    }
}
