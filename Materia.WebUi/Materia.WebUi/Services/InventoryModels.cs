using System.Text.Json.Serialization;

namespace Materia.WebUi.Services;

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
    List<UnitConversionDto> UnitConversions,
    List<CategoryRefDto> Categories,
    List<ColorVariantDto>? ColorVariants = null,
    decimal StockQuantity = 0m,
    decimal StockValue = 0m,
    decimal? LatestPurchasePrice = null,
    bool HasSupplier = false)
{
    public bool HasStock => StockQuantity > 0m;
    public bool HasPurchasePrice => LatestPurchasePrice is > 0m;
    public bool HasSalePrice => SalePrice > 0m;

    /// <summary>
    /// Mirrors <c>Materia.Application.DTOs.Inventory.ProductDto.NeedsAttention</c>: the
    /// product is missing a supplier, stock, harga beli, or harga jual.
    /// </summary>
    public bool NeedsAttention => !HasSupplier || !HasStock || !HasPurchasePrice || !HasSalePrice;
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

public record CategoryRefDto(Guid Id, string Name);

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt);

public record PagedProductsDto(
    List<ProductDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record UnitDto(
    Guid Id,
    string Name,
    string? Symbol,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt);

public record StockDto(
    Guid ProductId,
    decimal Quantity,
    string Unit,
    DateTime? LastAdjustedAt,
    string? LastAdjustedBy,
    // Set for a color-variant stock bucket; null for the product-level ("umum") bucket.
    Guid? VariantId = null,
    string? ColorName = null);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StockMovementType
{
    Initial,
    Adjustment,
    Sale,
    PurchaseReceipt,
    PurchaseReturn,
    UnitCorrection,
}

/// <summary>One line of the stock flow (kartu stok). Mirrors the API's StockMovementDto.</summary>
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

public record ProductSearchDto(
    Guid    Id,
    string  Name,
    string  Sku,
    string  BaseUnit,
    decimal SalePrice,
    string? Barcode,
    List<ProductUnitPriceDto> Units,
    List<ProductVariantSearchDto>? Variants = null);

public record ProductUnitPriceDto(string UnitName, decimal SalePrice);

public record ProductVariantSearchDto(
    Guid    VariantId,
    string  ColorName,
    string? ColorCode,
    string? Barcode,
    decimal EffectivePrice);
