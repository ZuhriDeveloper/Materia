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
    decimal Subtotal);

public record SalePaymentDto(
    decimal       PaidAmount,
    decimal       Change,
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
    decimal         Subtotal,
    decimal         GrandTotal,
    string          CreatedBy,
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
    decimal UnitPrice);

public record CheckoutRequest(
    decimal       PaidAmount,
    PaymentMethod PaymentMethod);

public record CancelSaleRequest(string Reason);
