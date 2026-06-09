using Materia.Domain.Sales;

namespace Materia.Application.Commands.Sales.FinalizeSale;

public record FinalizeSaleCommand(
    Guid?                                CustomerId,
    string?                              CustomerName,
    IReadOnlyList<FinalizeSaleItemInput> Items,
    bool                                 IsDeliveryRequired,
    string                               ServedBy,
    decimal                              Discount      = 0m,
    bool                                 TaxEnabled    = false,
    // Amount tendered up front. null ⇒ treat as full payment of the grand total.
    decimal?                             AmountPaid    = null,
    PaymentMethod                        PaymentMethod = PaymentMethod.Cash);

public record FinalizeSaleItemInput(
    Guid    ProductId,
    string  ProductName,
    string  UnitName,
    decimal Quantity,
    decimal UnitPrice,
    Guid?   VariantId = null,
    string? ColorName = null);

public record FinalizeSaleResult(
    Guid    SaleId,
    string  ReferenceNo,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal Change,
    decimal OutstandingAmount,
    bool    IsCredit);
