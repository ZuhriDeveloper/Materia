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
    List<ColorVariantDto>? ColorVariants = null);

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
    string? LastAdjustedBy);

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
