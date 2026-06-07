namespace Materia.Domain.Inventory;

/// <summary>
/// A sellable color variation of a <see cref="Product"/> — its own stock-keeping unit.
/// Each variant carries its own <see cref="Barcode"/> for scan-to-cart and an optional
/// <see cref="PriceOverride"/>; when the override is null the parent product's sale price
/// applies. Identity (<see cref="Id"/>) is stable so stock and sales can reference it.
/// </summary>
public sealed class ColorVariant
{
    public VariantId Id { get; }
    public Color Color { get; private set; }
    public string? Barcode { get; private set; }
    public decimal? PriceOverride { get; private set; }
    public bool IsActive { get; private set; }

    internal ColorVariant(
        VariantId id, Color color, string? barcode, decimal? priceOverride, bool isActive = true)
    {
        Id = id;
        Color = color;
        Barcode = barcode;
        PriceOverride = priceOverride;
        IsActive = isActive;
    }

    internal void Update(Color color, string? barcode, decimal? priceOverride)
    {
        Color = color;
        Barcode = barcode;
        PriceOverride = priceOverride;
    }

    internal void Deactivate() => IsActive = false;
    internal void Activate() => IsActive = true;
}
