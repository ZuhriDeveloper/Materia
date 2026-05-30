namespace Materia.Application.DTOs.Inventory;

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
    IReadOnlyList<UnitConversionDto> UnitConversions,
    IReadOnlyList<CategorySummaryDto> Categories);

public record UnitConversionDto(string FromUnit, string ToUnit, decimal Factor);

public record CategorySummaryDto(Guid Id, string Name);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
