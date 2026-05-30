using System.Text.Json;
using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class ProductQueryRepository(AppDbContext context) : IProductQueryRepository
{
    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var p = await context.ProductReadModels.FindAsync([id], ct);
        if (p is null) return null;

        var categories = await ResolveCategories(p.CategoryIdsJson, ct);
        return Map(p, categories);
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(
        int page, int pageSize, bool? isActive, CancellationToken ct = default)
    {
        var query = context.ProductReadModels.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var categoryIds = items
            .SelectMany(p => JsonSerializer.Deserialize<Guid[]>(p.CategoryIdsJson) ?? [])
            .Distinct()
            .ToList();

        var categoryMap = await context.CategoryReadModels
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var dtos = items.Select(p => Map(p, BuildCategories(p.CategoryIdsJson, categoryMap))).ToList();
        return new PagedResult<ProductDto>(dtos, total, page, pageSize);
    }

    private async Task<IReadOnlyList<CategorySummaryDto>> ResolveCategories(
        string categoryIdsJson, CancellationToken ct)
    {
        var ids = JsonSerializer.Deserialize<Guid[]>(categoryIdsJson) ?? [];
        if (ids.Length == 0) return [];

        return await context.CategoryReadModels
            .Where(c => ids.Contains(c.Id))
            .Select(c => new CategorySummaryDto(c.Id, c.Name))
            .ToListAsync(ct);
    }

    private static IReadOnlyList<CategorySummaryDto> BuildCategories(
        string json, Dictionary<Guid, string> map)
    {
        var ids = JsonSerializer.Deserialize<Guid[]>(json) ?? [];
        return ids
            .Where(map.ContainsKey)
            .Select(id => new CategorySummaryDto(id, map[id]))
            .ToList();
    }

    private static ProductDto Map(ProductReadModel p, IReadOnlyList<CategorySummaryDto> categories)
    {
        var conversions = JsonSerializer.Deserialize<ConversionJson[]>(p.UnitConversionsJson) ?? [];
        return new ProductDto(
            p.Id, p.Name, p.Description, p.BaseUnit, p.IsActive,
            p.CreatedBy, p.CreatedAt, p.UpdatedBy, p.UpdatedAt,
            conversions.Select(c => new UnitConversionDto(c.Value, c.ToUnit, c.Factor)).ToList(),
            categories);
    }

    private record ConversionJson(string Value, string ToUnit, decimal Factor);
}
