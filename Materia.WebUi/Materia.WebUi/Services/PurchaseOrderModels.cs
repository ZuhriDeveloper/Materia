namespace Materia.WebUi.Services;

public enum PurchaseOrderStatus { Draft, Confirmed, PartiallyReceived, Received, Cancelled }

public record PurchaseOrderLineDto(
    Guid    ProductId,
    string? ProductName,
    decimal OrderedQty,
    decimal ReceivedQty,
    decimal UnitCost,
    string  Unit);

public record PurchaseOrderDto(
    Guid                        Id,
    Guid                        SupplierId,
    string                      SupplierName,
    string                      Status,
    List<PurchaseOrderLineDto>  Lines,
    string                      CreatedBy,
    DateTime                    CreatedAt,
    DateTime?                   ReceivedAt);

// ── Request payloads ───────────────────────────────────────────────────────

public record CreatePoLineInput(Guid ProductId, decimal Qty);
public record ReceivePoLineInput(Guid ProductId, decimal ReceivedQty);
