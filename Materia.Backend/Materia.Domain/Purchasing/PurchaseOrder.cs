using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing.Events;

namespace Materia.Domain.Purchasing;

public enum PurchaseOrderStatus { Draft, Confirmed, PartiallyReceived, Received, Closed, Cancelled }

public sealed class PurchaseOrderLine
{
    public ProductId ProductId { get; init; } = default!;
    public decimal OrderedQty { get; init; }
    public decimal ReceivedQty { get; private set; }
    public decimal ReturnedQty { get; private set; }

    /// <summary>Net buy price (after any chained discount) — the basis for every cost calc.</summary>
    public decimal UnitCost { get; init; }

    /// <summary>List/base buy price before the chained discount. Equals <see cref="UnitCost"/> when no discount.</summary>
    public decimal ListUnitCost { get; init; }

    /// <summary>The chained trade discount applied to this line (% per level), or empty.</summary>
    public IReadOnlyList<decimal> Discounts { get; init; } = [];

    public string Unit { get; init; } = default!;

    /// <summary>Received goods still on hand for this PO — the basis for the amount owed.</summary>
    public decimal NetReceivedQty => ReceivedQty - ReturnedQty;

    internal void AddReceipt(decimal qty) => ReceivedQty += qty;
    internal void AddReturn(decimal qty) => ReturnedQty += qty;
}

public sealed class PurchaseOrder : AggregateRoot<PurchaseOrderId>
{
    public SupplierId SupplierId { get; private set; } = default!;
    public PurchaseOrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }

    /// <summary>Term of payment (tempo). Null = cash / no tempo.</summary>
    public PaymentTerm? PaymentTerm { get; private set; }

    /// <summary>When true, receiving goods updates the supplier catalog list price for each product.</summary>
    public bool UpdateCatalogOnReceipt { get; private set; }

    private readonly List<PurchaseOrderLine> _lines = [];
    public IReadOnlyList<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    private PurchaseOrder() { }

    // ── Factories ─────────────────────────────────────────────────────────────

    public static PurchaseOrder Create(
        SupplierId supplierId,
        IReadOnlyList<(ProductId ProductId, decimal Qty, decimal UnitCost, string Unit)> lines,
        string createdBy,
        PaymentTerm? paymentTerm = null,
        bool updateCatalogOnReceipt = false)
        => Create(
            supplierId,
            lines.Select(l =>
                (l.ProductId, l.Qty, l.UnitCost, (IReadOnlyList<decimal>)[], l.Unit)).ToList(),
            createdBy,
            paymentTerm,
            updateCatalogOnReceipt);

    public static PurchaseOrder Create(
        SupplierId supplierId,
        IReadOnlyList<(ProductId ProductId, decimal Qty, decimal ListUnitCost,
                       IReadOnlyList<decimal> Discounts, string Unit)> lines,
        string createdBy,
        PaymentTerm? paymentTerm = null,
        bool updateCatalogOnReceipt = false)
    {
        if (lines.Count == 0)
            throw new DomainException("Purchase order must have at least one line.");
        if (lines.Any(l => l.Qty <= 0))
            throw new DomainException("Ordered quantity must be positive.");
        if (lines.Any(l => l.ListUnitCost <= 0))
            throw new DomainException("Unit cost must be positive.");

        var lineData = lines.Select(l =>
        {
            var net = DiscountChain.ComputeNet(l.ListUnitCost, l.Discounts);
            if (net <= 0)
                throw new DomainException("Net unit cost after discount must be positive.");

            return new PurchaseOrderLineData(
                l.ProductId.Value, l.Qty, net, l.Unit, l.ListUnitCost, l.Discounts);
        }).ToList();

        var po = new PurchaseOrder();
        po.Raise(new PurchaseOrderCreated(
            PurchaseOrderId.New(),
            supplierId,
            lineData,
            createdBy,
            DateTime.UtcNow,
            paymentTerm?.Value,
            paymentTerm?.Unit.ToString(),
            updateCatalogOnReceipt));
        return po;
    }

    public static PurchaseOrder Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var po = new PurchaseOrder();
        po.Load(events);
        return po;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void Confirm(string confirmedBy)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException($"Only Draft POs can be confirmed. Current status: {Status}.");

        Raise(new PurchaseOrderConfirmed(Id, confirmedBy, DateTime.UtcNow));
    }

    public void Receive(
        IReadOnlyList<(ProductId ProductId, decimal ReceivedQty)> receipts,
        string receivedBy)
    {
        if (Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Closed or PurchaseOrderStatus.Cancelled)
            throw new DomainException($"PO cannot be received in status: {Status}.");

        foreach (var (productId, qty) in receipts)
        {
            var line = _lines.FirstOrDefault(l => l.ProductId == productId)
                ?? throw new DomainException($"Product {productId.Value} not found in this PO.");

            if (qty <= 0)
                throw new DomainException("Received quantity must be positive.");
        }

        Raise(new PurchaseOrderReceived(
            Id,
            receipts.Select(r => new ReceiptLineData(r.ProductId.Value, r.ReceivedQty)).ToList(),
            receivedBy,
            DateTime.UtcNow));
    }

    public void RecordReturn(
        IReadOnlyList<(ProductId ProductId, decimal Qty)> returns,
        string reason,
        string returnedBy)
    {
        if (Status is not (PurchaseOrderStatus.PartiallyReceived
                           or PurchaseOrderStatus.Received
                           or PurchaseOrderStatus.Closed))
            throw new DomainException(
                $"Only received goods can be returned. Current status: {Status}.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Return reason is required.");
        if (returns.Count == 0)
            throw new DomainException("At least one return line is required.");

        foreach (var (productId, qty) in returns)
        {
            var line = _lines.FirstOrDefault(l => l.ProductId == productId)
                ?? throw new DomainException($"Product {productId.Value} not found in this PO.");

            if (qty <= 0)
                throw new DomainException("Returned quantity must be positive.");
            if (qty > line.NetReceivedQty)
                throw new DomainException(
                    $"Cannot return {qty} — only {line.NetReceivedQty} received and on hand for this product.");
        }

        Raise(new PurchaseOrderReturned(
            Id,
            returns.Select(r => new ReturnLineData(r.ProductId.Value, r.Qty)).ToList(),
            reason,
            returnedBy,
            DateTime.UtcNow));
    }

    public void Close(string reason, string closedBy)
    {
        // Short-close: supplier won't ship the rest. Only meaningful once something was received.
        if (Status != PurchaseOrderStatus.PartiallyReceived)
            throw new DomainException(
                $"Only partially-received POs can be closed. Current status: {Status}. " +
                "Use Cancel for an order with no receipts.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Close reason is required.");

        Raise(new PurchaseOrderClosed(Id, reason, closedBy, DateTime.UtcNow));
    }

    public void Cancel(string reason, string cancelledBy)
    {
        // Once any goods are received the whole PO can no longer be voided — use Return or Close.
        if (Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Confirmed))
            throw new DomainException($"PO cannot be cancelled in status: {Status}.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required.");

        Raise(new PurchaseOrderCancelled(Id, reason, cancelledBy, DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case PurchaseOrderCreated e:
                Id = e.PurchaseOrderId;
                SupplierId = e.SupplierId;
                Status = PurchaseOrderStatus.Draft;
                CreatedAt = e.OccurredAt;
                PaymentTerm = PaymentTerm.FromRaw(e.PaymentTermValue, e.PaymentTermUnit);
                UpdateCatalogOnReceipt = e.UpdateCatalogOnReceipt;
                _lines.AddRange(e.Lines.Select(l => new PurchaseOrderLine
                {
                    ProductId = ProductId.From(l.ProductId),
                    OrderedQty = l.OrderedQty,
                    UnitCost = l.UnitCost,
                    ListUnitCost = l.ListUnitCost ?? l.UnitCost,
                    Discounts = l.Discounts ?? [],
                    Unit = l.Unit,
                }));
                break;

            case PurchaseOrderConfirmed:
                Status = PurchaseOrderStatus.Confirmed;
                break;

            case PurchaseOrderReceived e:
                foreach (var receipt in e.Lines)
                {
                    var line = _lines.First(l => l.ProductId.Value == receipt.ProductId);
                    line.AddReceipt(receipt.ReceivedQty);
                }
                var allFulfilled = _lines.All(l => l.ReceivedQty >= l.OrderedQty);
                Status = allFulfilled ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
                ReceivedAt = allFulfilled ? e.OccurredAt : null;
                break;

            case PurchaseOrderReturned e:
                foreach (var ret in e.Lines)
                {
                    var line = _lines.First(l => l.ProductId.Value == ret.ProductId);
                    line.AddReturn(ret.ReturnedQty);
                }
                break;

            case PurchaseOrderClosed e:
                Status = PurchaseOrderStatus.Closed;
                ReceivedAt = e.OccurredAt;
                break;

            case PurchaseOrderCancelled:
                Status = PurchaseOrderStatus.Cancelled;
                break;
        }
    }
}
