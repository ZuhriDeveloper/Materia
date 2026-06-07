namespace Materia.Application.Contracts.Inventory;

/// <summary>
/// Fast product-name lookup for the PoS cashier autocomplete, served from a cache
/// (Redis) rather than hitting the database on every keystroke.
/// </summary>
public interface IProductSearchCache
{
    Task<IReadOnlyList<ProductSearchResult>> SearchAsync(
        string term, int limit, CancellationToken ct = default);

    /// <summary>Drops the cached catalog so the next search rebuilds it from the source.</summary>
    Task InvalidateAsync(CancellationToken ct = default);
}

public record ProductSearchResult(
    Guid    Id,
    string  Name,
    string  Sku,
    string  BaseUnit,
    decimal SalePrice,
    string? Barcode,
    IReadOnlyList<ProductUnitPrice> Units,
    IReadOnlyList<ProductVariantSearch>? Variants = null);

/// <summary>A sellable unit for a product and its price (base unit + each conversion unit).</summary>
public record ProductUnitPrice(string UnitName, decimal SalePrice);

/// <summary>An active color variant of a product, for PoS selection and barcode scanning.</summary>
public record ProductVariantSearch(
    Guid    VariantId,
    string  ColorName,
    string? ColorCode,
    string? Barcode,
    decimal EffectivePrice);
