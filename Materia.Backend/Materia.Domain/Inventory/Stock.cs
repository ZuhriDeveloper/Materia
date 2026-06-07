using Materia.Domain.Common;
using Materia.Domain.Inventory.Events;
using Materia.Domain.Purchasing;

namespace Materia.Domain.Inventory;

public class Stock : AggregateRoot<StockId>
{
    public ProductId ProductId { get; private set; } = default!;

    /// <summary>The color variant this stock is for, or null for product-level stock.</summary>
    public VariantId? VariantId { get; private set; }

    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = default!;

    private Stock() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Stock Initialize(
        ProductId productId, string unit, string createdBy, VariantId? variantId = null)
    {
        if (string.IsNullOrWhiteSpace(unit))
            throw new DomainException("Stock unit cannot be empty.");

        var stock = new Stock();
        stock.Raise(new StockInitialized(
            StockId.New(), productId, 0m, unit, createdBy, DateTime.UtcNow, variantId?.Value));
        return stock;
    }

    public static Stock Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var stock = new Stock();
        stock.Load(events);
        return stock;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void Adjust(decimal delta, string? reason, string adjustedBy)
    {
        var newQuantity = Quantity + delta;
        Raise(new StockAdjusted(Id, ProductId, delta, newQuantity, reason, adjustedBy, DateTime.UtcNow));
    }

    public void ReconcileFromPurchase(
        decimal receivedQty,
        PurchaseOrderId purchaseOrderId,
        decimal unitCost,
        string unit,
        string reconciledBy)
    {
        if (receivedQty <= 0)
            throw new DomainException("Received quantity must be positive.");

        var newQuantity = Quantity + receivedQty;
        Raise(new StockReconciledFromPurchase(
            Id, ProductId, receivedQty, newQuantity,
            purchaseOrderId, unitCost, unit, reconciledBy, DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case StockInitialized e:
                Id = e.StockId;
                ProductId = e.ProductId;
                VariantId = e.VariantId is { } v ? Inventory.VariantId.From(v) : null;
                Quantity = e.Quantity;
                Unit = e.Unit;
                break;

            case StockAdjusted e:
                Quantity = e.NewQuantity;
                break;

            case StockReconciledFromPurchase e:
                Quantity = e.NewQuantity;
                break;
        }
    }
}
