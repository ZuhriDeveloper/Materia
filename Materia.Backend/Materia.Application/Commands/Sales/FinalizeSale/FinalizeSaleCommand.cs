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

/// <summary>
/// One line item in a consumer finalize-sale request.
/// </summary>
/// <param name="UnitPrice">
/// Net (as-charged) unit price — what the customer actually pays per unit.
/// Used as the list price when <paramref name="ListUnitPrice"/> is not supplied (back-compat).
/// </param>
/// <param name="ListUnitPrice">
/// Gross list price before line discount. When provided it must be ≥ <paramref name="UnitPrice"/>.
/// When omitted, defaults to <paramref name="UnitPrice"/> (no discount scenario).
/// </param>
/// <param name="DiscountPerUnit">
/// Per-unit discount off <paramref name="ListUnitPrice"/>.
/// Must satisfy: 0 ≤ DiscountPerUnit ≤ ListUnitPrice.
/// </param>
public record FinalizeSaleItemInput(
    Guid     ProductId,
    string   ProductName,
    string   UnitName,
    decimal  Quantity,
    decimal  UnitPrice,
    Guid?    VariantId       = null,
    string?  ColorName       = null,
    decimal? ListUnitPrice   = null,
    decimal? DiscountPerUnit = null);

public record FinalizeSaleResult(
    Guid    SaleId,
    string  ReferenceNo,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal Change,
    decimal OutstandingAmount,
    bool    IsCredit);
