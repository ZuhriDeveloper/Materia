namespace Materia.WebUi.Services;

public enum PurchaseOrderStatus { Draft, Confirmed, PartiallyReceived, Received, Cancelled }

public enum PaymentTermUnit { Days, Weeks, Months }

public record PurchaseOrderLineDto(
    Guid    ProductId,
    string? ProductName,
    decimal OrderedQty,
    decimal ReceivedQty,
    decimal ReturnedQty,
    decimal UnitCost,
    string  Unit)
{
    /// <summary>Received goods still on hand for this PO — the basis for the amount owed.</summary>
    public decimal NetReceivedQty => ReceivedQty - ReturnedQty;
}

public record PurchaseOrderDto(
    Guid                        Id,
    Guid                        SupplierId,
    string                      SupplierName,
    string                      Status,
    List<PurchaseOrderLineDto>  Lines,
    string                      CreatedBy,
    DateTime                    CreatedAt,
    DateTime?                   ReceivedAt,
    int?                        PaymentTermValue,
    string?                     PaymentTermUnit,
    DateTime?                   DueDate);

// ── Request payloads ───────────────────────────────────────────────────────

public record CreatePoLineInput(Guid ProductId, decimal Qty);
public record ReceivePoLineInput(Guid ProductId, decimal ReceivedQty);
public record ReturnPoLineInput(Guid ProductId, decimal ReturnedQty);
