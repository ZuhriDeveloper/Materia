namespace Materia.Application.Commands.Sales.FinalizeSale;

public record FinalizeSaleCommand(
    Guid?                                CustomerId,
    string?                              CustomerName,
    IReadOnlyList<FinalizeSaleItemInput> Items,
    bool                                 IsDeliveryRequired,
    string                               ServedBy);

public record FinalizeSaleItemInput(
    Guid    ProductId,
    string  ProductName,
    string  UnitName,
    decimal Quantity,
    decimal UnitPrice,
    Guid?   VariantId = null,
    string? ColorName = null);

public record FinalizeSaleResult(Guid SaleId, string ReferenceNo);
