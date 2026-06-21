namespace Materia.Application.Contracts.Sales;

public interface ISaleReturnQueryRepository
{
    Task<IReadOnlyList<SaleReturnSummary>> GetBySaleIdAsync(Guid saleId, CancellationToken ct = default);
}

public record SaleReturnSummary(
    Guid     ReturnId,
    string   OriginalReferenceNo,
    decimal  TotalRefundAmount,
    string   Resolution,
    string   Reason,
    string   ReturnedBy,
    DateTime ReturnedAt,
    IReadOnlyList<SaleReturnLineSummary> Lines);

public record SaleReturnLineSummary(
    Guid    ProductId,
    string  ProductName,
    Guid?   VariantId,
    string? ColorName,
    string  UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Subtotal);
