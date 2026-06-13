namespace Materia.Application.DTOs.Inventory;

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string BaseUnit,
    decimal SalePrice,
    string? Barcode,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt,
    IReadOnlyList<UnitConversionDto> UnitConversions,
    IReadOnlyList<CategorySummaryDto> Categories,
    IReadOnlyList<ColorVariantDto> ColorVariants,
    // On-hand product-level stock (variant stock excluded — deferred). 0 when no stock record exists.
    decimal StockQuantity = 0m,
    // Most recent purchase price (harga beli) across supplier catalogs; null when none is recorded.
    decimal? LatestPurchasePrice = null,
    // True when at least one supplier catalogs this product.
    bool HasSupplier = false)
{
    /// <summary>
    /// Flags a product whose master data is incomplete and needs attention: it has no
    /// supplier, no stock on hand, no purchase price (harga beli), or no sale price (harga jual).
    /// Surfaced in the admin product list so staff can complete the missing data.
    /// </summary>
    public bool NeedsAttention =>
        !HasSupplier
        || StockQuantity <= 0m
        || LatestPurchasePrice is null or <= 0m
        || SalePrice <= 0m;
}

public record UnitConversionDto(string FromUnit, string ToUnit, decimal Factor, decimal SalePrice);

public record ColorVariantDto(
    Guid Id,
    string ColorName,
    string? ColorCode,
    string? Barcode,
    decimal? PriceOverride,
    decimal EffectivePrice,
    bool IsActive);

public record CategorySummaryDto(Guid Id, string Name);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
