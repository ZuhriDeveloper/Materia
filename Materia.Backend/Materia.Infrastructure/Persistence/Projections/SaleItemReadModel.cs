namespace Materia.Infrastructure.Persistence.Projections;

public class SaleItemReadModel
{
    public Guid    Id                 { get; set; }
    public Guid    StoreId            { get; set; }
    public Guid    SaleId             { get; set; }
    public Guid    ProductId          { get; set; }
    public Guid?   VariantId          { get; set; }
    public string  ProductName        { get; set; } = default!;
    public string? ColorName          { get; set; }
    public string  UnitName           { get; set; } = default!;
    public decimal Quantity           { get; set; }
    public decimal QuantityInBaseUnit { get; set; }

    // ── Pricing (Phase 1) ────────────────────────────────────────────────────

    /// <summary>Gross list price before line discount. Equals UnitPrice when no discount.</summary>
    public decimal ListUnitPrice    { get; set; }
    /// <summary>Per-unit discount applied to this line. Zero when no discount.</summary>
    public decimal DiscountPerUnit  { get; set; }
    /// <summary>Net (as-charged) unit price: ListUnitPrice − DiscountPerUnit.</summary>
    public decimal NetUnitPrice     { get; set; }
    /// <summary>Back-compat: same as NetUnitPrice.</summary>
    public decimal UnitPrice        { get; set; }
    /// <summary>Net line subtotal (NetUnitPrice × Quantity).</summary>
    public decimal Subtotal         { get; set; }
    /// <summary>Gross line subtotal (ListUnitPrice × Quantity).</summary>
    public decimal GrossSubtotal    { get; set; }
    /// <summary>Line discount total (GrossSubtotal − Subtotal).</summary>
    public decimal LineDiscount     { get; set; }

    // ── Cost (Phase 1) ───────────────────────────────────────────────────────

    /// <summary>Snapshotted unit cost at time of finalization (per base unit). Zero if unknown.</summary>
    public decimal UnitCost         { get; set; }
    /// <summary>Total line cost (UnitCost × QuantityInBaseUnit).</summary>
    public decimal LineCost         { get; set; }
}
