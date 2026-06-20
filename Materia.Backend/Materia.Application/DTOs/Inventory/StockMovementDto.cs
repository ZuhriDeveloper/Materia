using System.Text.Json.Serialization;

namespace Materia.Application.DTOs.Inventory;

/// <summary>Kind of stock movement, derived from the underlying domain event.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StockMovementType
{
    /// <summary>Stock bucket created (opening balance of 0).</summary>
    Initial,
    /// <summary>Manual adjustment / stock opname (not tied to a sale or purchase).</summary>
    Adjustment,
    /// <summary>Deduction from a sale.</summary>
    Sale,
    /// <summary>Goods received against a purchase order.</summary>
    PurchaseReceipt,
    /// <summary>Goods returned to the supplier.</summary>
    PurchaseReturn,
    /// <summary>Base-unit correction (quantity unchanged).</summary>
    UnitCorrection,
}

/// <summary>
/// One line in the stock flow (kartu stok): the signed quantity change, the resulting on-hand
/// balance, and the running weighted-average cost / value at that point. Bucket-aware:
/// <see cref="VariantId"/> / <see cref="ColorName"/> identify the color variant, null for the
/// product-level ("umum") bucket.
/// </summary>
public record StockMovementDto(
    DateTime OccurredAt,
    StockMovementType Type,
    decimal Delta,
    decimal BalanceAfter,
    string Unit,
    string? Reason,
    string? Reference,
    string PerformedBy,
    decimal? UnitCost,
    decimal RunningAverageCost,
    decimal BalanceValue,
    Guid? VariantId = null,
    string? ColorName = null);
