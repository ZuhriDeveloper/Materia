using Materia.Domain.Common;

namespace Materia.Domain.Sales;

public sealed class SaleItem
{
    public SaleItemId Id                 { get; }
    public Guid       ProductId          { get; }
    public Guid?      VariantId          { get; }   // color variant sold, or null
    public string     ProductName        { get; }
    public string?    ColorName          { get; }   // color label for receipts, or null
    public string     UnitName           { get; }   // unit as entered (e.g. "sak")
    public decimal    Quantity           { get; }   // quantity in entered unit
    public decimal    QuantityInBaseUnit { get; }   // converted to product base unit

    // ── Pricing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Gross list price before any line-level discount.
    /// Equals <see cref="UnitPrice"/> when no line discount was applied (back-compat).
    /// </summary>
    public Money ListUnitPrice   { get; }

    /// <summary>Per-unit discount off <see cref="ListUnitPrice"/>. Zero by default.</summary>
    public Money DiscountPerUnit { get; }

    /// <summary>
    /// Net (as-charged) unit price: <c>max(0, ListUnitPrice − DiscountPerUnit)</c>.
    /// Equals <see cref="ListUnitPrice"/> when there is no discount.
    /// </summary>
    public Money NetUnitPrice    => new(Math.Max(0m, ListUnitPrice.Amount - DiscountPerUnit.Amount));

    /// <summary>
    /// Back-compat alias: the effective/net price the customer pays.
    /// Identical to <see cref="NetUnitPrice"/>.
    /// </summary>
    public Money UnitPrice       => NetUnitPrice;

    /// <summary>Net subtotal for this line: <see cref="NetUnitPrice"/> × <see cref="Quantity"/>.</summary>
    public Money Subtotal        => NetUnitPrice.Multiply(Quantity);

    /// <summary>Gross subtotal before line discount: <see cref="ListUnitPrice"/> × <see cref="Quantity"/>.</summary>
    public Money GrossSubtotal   => ListUnitPrice.Multiply(Quantity);

    /// <summary>Total line discount amount: <see cref="GrossSubtotal"/> − <see cref="Subtotal"/>.</summary>
    public Money LineDiscount    => new(GrossSubtotal.Amount - Subtotal.Amount);

    // ── Cost & margin ─────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshotted supplier unit cost (per base unit) at time of sale.
    /// Zero when no purchase price exists for the product.
    /// </summary>
    public Money UnitCost        { get; }

    /// <summary>
    /// Total line cost: <see cref="UnitCost"/> × <see cref="QuantityInBaseUnit"/>.
    /// Uses base-unit quantity so cost matches the stock deduction unit.
    /// </summary>
    public Money LineCost        => UnitCost.Multiply(QuantityInBaseUnit);

    /// <summary>
    /// Line-level gross margin (decimal, not Money — can be negative).
    /// <c>Subtotal − LineCost</c>.
    /// </summary>
    public decimal LineMargin    => Subtotal.Amount - LineCost.Amount;

    // ── Promotion ─────────────────────────────────────────────────────────────

    public Guid?   AppliedPromotionId { get; }
    public string? PromotionLabel     { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    internal SaleItem(
        SaleItemId id,
        Guid       productId,
        string     productName,
        string     unitName,
        decimal    quantity,
        decimal    quantityInBaseUnit,
        Money      listUnitPrice,
        Money      discountPerUnit,
        Money      unitCost,
        Guid?      variantId         = null,
        string?    colorName         = null,
        Guid?      appliedPromotionId = null,
        string?    promotionLabel    = null)
    {
        if (quantity <= 0)
            throw new DomainException("Kuantitas harus lebih dari nol.");
        if (quantityInBaseUnit <= 0)
            throw new DomainException("Kuantitas satuan dasar harus lebih dari nol.");

        Id                 = id;
        ProductId          = productId;
        VariantId          = variantId;
        ProductName        = productName;
        ColorName          = colorName;
        UnitName           = unitName;
        Quantity           = quantity;
        QuantityInBaseUnit = quantityInBaseUnit;
        ListUnitPrice      = listUnitPrice;
        DiscountPerUnit    = discountPerUnit;
        UnitCost           = unitCost;
        AppliedPromotionId = appliedPromotionId;
        PromotionLabel     = promotionLabel;
    }
}
