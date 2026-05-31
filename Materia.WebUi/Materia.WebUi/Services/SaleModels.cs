namespace Materia.WebUi.Services;

public enum SaleType      { Pickup, Delivery }
public enum SaleStatus    { Draft, Confirmed, Paid, Cancelled }
public enum PaymentMethod { Cash, BankTransfer, QRIS, Debit, Credit }

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
    List<SaleItemDto> Items);

public record PagedSalesDto(
    List<SaleDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
