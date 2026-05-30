namespace Materia.WebUi.Services;

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string BaseUnit,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt,
    List<UnitConversionDto> UnitConversions,
    List<CategoryRefDto> Categories);

public record UnitConversionDto(string FromUnit, string ToUnit, decimal Factor);

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
