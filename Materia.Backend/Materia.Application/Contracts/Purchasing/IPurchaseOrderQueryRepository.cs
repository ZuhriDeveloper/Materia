namespace Materia.Application.Contracts.Purchasing;

public interface IPurchaseOrderQueryRepository
{
    Task<IReadOnlyList<PurchaseOrderDto>> GetAllAsync(CancellationToken ct = default);
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public record PurchaseOrderDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string Status,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? ReceivedAt);

public record PurchaseOrderLineDto(
    Guid ProductId,
    string? ProductName,
    decimal OrderedQty,
    decimal ReceivedQty,
    decimal UnitCost,
    string Unit);
