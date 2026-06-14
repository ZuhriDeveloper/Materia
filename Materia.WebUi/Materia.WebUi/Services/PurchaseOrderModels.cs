using System.Globalization;

namespace Materia.WebUi.Services;

public enum PurchaseOrderStatus { Draft, Confirmed, PartiallyReceived, Received, Cancelled }

/// <summary>Parsing / formatting / preview for chained trade discounts (e.g. "12,5+7+5").</summary>
public static class DiscountInput
{
    /// <summary>Parses free text like "12,5+7+5" into discount levels. Accepts "," or "." decimals.</summary>
    public static List<decimal> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return text
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => decimal.TryParse(part.Replace(',', '.'),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null)
            .OfType<decimal>()
            .ToList();
    }

    /// <summary>Net price after folding the chain — mirrors the server's DiscountChain.ComputeNet.</summary>
    public static decimal ComputeNet(decimal list, IReadOnlyList<decimal> discounts)
        => discounts.Count == 0
            ? list
            : Math.Round(discounts.Aggregate(list, (running, d) => running * (1m - d / 100m)),
                         2, MidpointRounding.AwayFromZero);

    /// <summary>Human chain like "12,5%+7%+5%".</summary>
    public static string Format(IReadOnlyList<decimal>? discounts)
        => discounts is null || discounts.Count == 0
            ? ""
            : string.Join("+", discounts.Select(d =>
                d.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',') + "%"));
}

public enum PaymentTermUnit { Days, Weeks, Months }

public record PurchaseOrderLineDto(
    Guid    ProductId,
    string? ProductName,
    decimal OrderedQty,
    decimal ReceivedQty,
    decimal ReturnedQty,
    decimal UnitCost,
    string  Unit,
    decimal ListUnitCost = 0m,
    IReadOnlyList<decimal>? Discounts = null)
{
    /// <summary>Received goods still on hand for this PO — the basis for the amount owed.</summary>
    public decimal NetReceivedQty => ReceivedQty - ReturnedQty;

    /// <summary>True when a chained discount was applied (net UnitCost is below the list price).</summary>
    public bool HasDiscount => Discounts is { Count: > 0 };
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

public record CreatePoLineInput(
    Guid ProductId, decimal Qty, IReadOnlyList<decimal>? Discounts = null);
public record ReceivePoLineInput(Guid ProductId, decimal ReceivedQty);
public record ReturnPoLineInput(Guid ProductId, decimal ReturnedQty);
