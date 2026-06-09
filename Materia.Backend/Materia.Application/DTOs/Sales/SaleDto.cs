using Materia.Domain.Sales;

namespace Materia.Application.DTOs.Sales;

public record SaleItemDto(
    Guid    Id,
    Guid    ProductId,
    string  ProductName,
    string  UnitName,
    decimal Quantity,
    decimal QuantityInBaseUnit,
    decimal UnitPrice,
    decimal Subtotal,
    Guid?   VariantId = null,
    string? ColorName = null);

public record SalePaymentDto(
    decimal       PaidAmount,
    decimal       Change,
    decimal       Outstanding,
    PaymentMethod Method,
    DateTime      PaidAt);

public record SaleDto(
    Guid            Id,
    string          ReferenceNo,
    Guid?           CustomerId,
    string          CustomerName,
    Guid?           CustomerAddressId,
    string?         DeliveryAddress,
    SaleType        SaleType,
    SaleStatus      Status,
    bool            IsDeliveryRequired,
    decimal         Subtotal,
    decimal         Discount,
    decimal         Tax,
    decimal         GrandTotal,
    decimal         AmountPaid,
    decimal         OutstandingAmount,
    string          CreatedBy,
    string?         ServedBy,
    DateTime        CreatedAt,
    SalePaymentDto? Payment,
    IReadOnlyList<SaleItemDto> Items);

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateSaleRequest(
    Guid?   CustomerId,
    string  CustomerName,
    string  SaleType,
    Guid?   CustomerAddressId,
    string? DeliveryAddress);

public record AddSaleItemRequest(
    Guid    ProductId,
    string  ProductName,
    string  UnitName,
    decimal Quantity,
    decimal UnitPrice,
    Guid?   VariantId = null,
    string? ColorName = null);

public record CheckoutRequest(
    decimal       PaidAmount,
    PaymentMethod PaymentMethod);

public record CancelSaleRequest(string Reason);
