using Materia.Domain.Common;

namespace Materia.Domain.Sales.Events;

public record SaleReturnRecorded(
    SaleReturnId                     ReturnId,
    SaleId                           OriginalSaleId,
    string                           OriginalReferenceNo,
    IReadOnlyList<SaleReturnLineData> Lines,
    decimal                          TotalRefundAmount,
    ReturnResolution                 Resolution,
    string                           Reason,
    string                           ReturnedBy,
    DateTime                         OccurredAt) : IDomainEvent;

public record SaleReturnLineData(
    Guid    ProductId,
    string  ProductName,
    Guid?   VariantId,
    string? ColorName,
    string  UnitName,
    decimal Quantity,
    decimal QuantityInBaseUnit,
    decimal UnitPrice);
