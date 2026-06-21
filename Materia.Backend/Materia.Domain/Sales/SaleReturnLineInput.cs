namespace Materia.Domain.Sales;

public sealed record SaleReturnLineInput(
    Guid    ProductId,
    string  ProductName,
    Guid?   VariantId,
    string? ColorName,
    string  UnitName,
    decimal Quantity,
    decimal QuantityInBaseUnit,
    decimal UnitPrice);
