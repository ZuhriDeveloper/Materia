namespace Materia.Application.Commands.Sales.RecordSaleReturn;

public sealed record RecordSaleReturnCommand(
    Guid                         OriginalSaleId,
    IReadOnlyList<ReturnLineDto> Lines,
    string                       Resolution,
    string                       Reason,
    string                       ReturnedBy);

public sealed record ReturnLineDto(
    Guid    ProductId,
    Guid?   VariantId,
    string  UnitName,
    decimal Quantity);

public sealed record RecordSaleReturnResult(
    Guid    ReturnId,
    string  OriginalReferenceNo,
    decimal TotalRefundAmount,
    string  Resolution);
