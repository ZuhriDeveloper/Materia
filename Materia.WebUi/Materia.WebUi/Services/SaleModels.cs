namespace Materia.WebUi.Services;

public enum SaleType      { Pickup, Delivery }

// Mirrors Materia.Domain.Sales enums. Sent over the API by ordinal — keep in sync, append only.
public enum SaleStatus    { Draft, Confirmed, Paid, Cancelled, PartiallyPaid }
public enum PaymentMethod { Cash, BankTransfer, QRIS, Debit, Credit }

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
    List<SaleItemDto> Items,
    decimal         ReturnedAmount = 0m);

public record PagedSalesDto(
    List<SaleDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

// ── Consumer sale (single-step finalize) ────────────────────────────────────

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

// ── Sale Return ──────────────────────────────────────────────────────────────

public enum ReturnResolution { CashRefund, DebtReduction }

public record SaleReturnLineSummaryDto(
    Guid    ProductId,
    string  ProductName,
    Guid?   VariantId,
    string? ColorName,
    string  UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public record SaleReturnSummaryDto(
    Guid   ReturnId,
    string OriginalReferenceNo,
    decimal TotalRefundAmount,
    string  Resolution,
    string  Reason,
    string  ReturnedBy,
    DateTime ReturnedAt,
    IReadOnlyList<SaleReturnLineSummaryDto> Lines);

public record RecordReturnLineDto(
    Guid    ProductId,
    Guid?   VariantId,
    string  UnitName,
    decimal Quantity);

public record RecordReturnResult(
    Guid    ReturnId,
    string  OriginalReferenceNo,
    decimal TotalRefundAmount,
    string  Resolution);
