using Materia.Domain.Common;
using Materia.Domain.Inventory.Events;

namespace Materia.Domain.Inventory;

public class Stock : AggregateRoot<StockId>
{
    public ProductId ProductId { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = default!;

    private Stock() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Stock Initialize(ProductId productId, string unit, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(unit))
            throw new DomainException("Stock unit cannot be empty.");

        var stock = new Stock();
        stock.Raise(new StockInitialized(StockId.New(), productId, 0m, unit, createdBy, DateTime.UtcNow));
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
        if (newQuantity < 0)
            throw new DomainException($"Stock cannot go below zero. Current: {Quantity}, Delta: {delta}.");

        Raise(new StockAdjusted(Id, ProductId, delta, newQuantity, reason, adjustedBy, DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case StockInitialized e:
                Id = e.StockId;
                ProductId = e.ProductId;
                Quantity = e.Quantity;
                Unit = e.Unit;
                break;

            case StockAdjusted e:
                Quantity = e.NewQuantity;
                break;
        }
    }
}
