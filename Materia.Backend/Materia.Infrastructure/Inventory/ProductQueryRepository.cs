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

        var variantRows = await context.ProductVariantReadModels
            .Where(v => v.ProductId == id)
            .OrderBy(v => v.ColorName)
            .ToListAsync(ct);
        var variants = variantRows.Select(v => ToVariantDto(v, p.SalePrice)).ToList();

        var stock = await context.StockReadModels
            .Where(s => s.ProductId == id && s.VariantId == null)
            .Select(s => (decimal?)s.Quantity)
            .FirstOrDefaultAsync(ct);
        var purchaseInfo = await BuildPurchaseInfoAsync([id], ct);
        var (latestPrice, hasSupplier) = purchaseInfo.GetValueOrDefault(id);

        return Map(p, categories, variants) with
        {
            StockQuantity = stock ?? 0m,
            LatestPurchasePrice = latestPrice,
            HasSupplier = hasSupplier,
        };
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(
        int page, int pageSize, bool? isActive,
        string? search = null, Guid? categoryId = null,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(context.ProductReadModels.AsQueryable(), isActive, search, categoryId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = await EnrichAsync(items, ct);
        return new PagedResult<ProductDto>(dtos, total, page, pageSize);
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(
        bool? isActive, string? search = null, Guid? categoryId = null,
        CancellationToken ct = default)
    {
        var items = await ApplyFilters(context.ProductReadModels.AsQueryable(), isActive, search, categoryId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return await EnrichAsync(items, ct);
    }

    private static IQueryable<ProductReadModel> ApplyFilters(
        IQueryable<ProductReadModel> query, bool? isActive, string? search, Guid? categoryId)
    {
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(s)));
        }

        if (categoryId.HasValue)
        {
            // CategoryIdsJson is a JSON array of GUID strings; a simple Contains is
            // safe because GUIDs are 36-char unique strings with no substring overlap.
            var catStr = categoryId.Value.ToString();
            query = query.Where(p => p.CategoryIdsJson.Contains(catStr));
        }

        return query;
    }

    /// <summary>
    /// Enriches read-model rows with categories, color variants, product-level stock, and the
    /// latest purchase price / supplier presence, then maps each to a <see cref="ProductDto"/>.
    /// </summary>
    private async Task<List<ProductDto>> EnrichAsync(List<ProductReadModel> items, CancellationToken ct)
    {
        var categoryIds = items
            .SelectMany(p => JsonSerializer.Deserialize<Guid[]>(p.CategoryIdsJson) ?? [])
            .Distinct()
            .ToList();

        var categoryMap = await context.CategoryReadModels
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var productIds = items.Select(p => p.Id).ToList();
        var variantRows = await context.ProductVariantReadModels
            .Where(v => productIds.Contains(v.ProductId))
            .ToListAsync(ct);
        var variantsByProduct = variantRows
            .GroupBy(v => v.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.ColorName).ToList());

        // Product-level stock (variant rows excluded — variant stock is deferred).
        var stockByProduct = await context.StockReadModels
            .Where(s => productIds.Contains(s.ProductId) && s.VariantId == null)
            .ToDictionaryAsync(s => s.ProductId, s => s.Quantity, ct);

        // Latest purchase price (harga beli) and supplier presence, from supplier catalogs.
        var purchaseInfo = await BuildPurchaseInfoAsync(productIds, ct);

        return items.Select(p =>
        {
            var dto = Map(
                p,
                BuildCategories(p.CategoryIdsJson, categoryMap),
                (variantsByProduct.TryGetValue(p.Id, out var vs) ? vs : [])
                    .Select(v => ToVariantDto(v, p.SalePrice)).ToList());
            var (latestPrice, hasSupplier) = purchaseInfo.GetValueOrDefault(p.Id);
            return dto with
            {
                StockQuantity = stockByProduct.GetValueOrDefault(p.Id),
                LatestPurchasePrice = latestPrice,
                HasSupplier = hasSupplier,
            };
        }).ToList();
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

    private static ProductDto Map(
        ProductReadModel p,
        IReadOnlyList<CategorySummaryDto> categories,
        IReadOnlyList<ColorVariantDto> colorVariants)
    {
        var conversions = JsonSerializer.Deserialize<ConversionJson[]>(p.UnitConversionsJson) ?? [];
        return new ProductDto(
            p.Id, p.Name, p.Description, p.BaseUnit, p.SalePrice, p.Barcode, p.IsActive,
            p.CreatedBy, p.CreatedAt, p.UpdatedBy, p.UpdatedAt,
            conversions.Select(c => new UnitConversionDto(c.Value, c.ToUnit, c.Factor, c.SalePrice)).ToList(),
            categories,
            colorVariants);
    }

    private static ColorVariantDto ToVariantDto(ProductVariantReadModel v, decimal productSalePrice)
        => new(v.Id, v.ColorName, v.ColorCode, v.Barcode, v.PriceOverride,
               v.PriceOverride ?? productSalePrice, v.IsActive);

    /// <summary>
    /// For the given products, derives whether any supplier catalogs them and the most
    /// recent purchase price (harga beli) across all suppliers. Suppliers are few, so a
    /// single in-memory scan of their catalog JSON is cheaper than per-product queries.
    /// </summary>
    private async Task<Dictionary<Guid, (decimal? LatestPurchasePrice, bool HasSupplier)>>
        BuildPurchaseInfoAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct)
    {
        if (productIds.Count == 0) return [];

        var wanted = productIds.ToHashSet();
        var catalogs = await context.SupplierReadModels
            .Select(s => s.CatalogJson)
            .ToListAsync(ct);

        // Per product, keep the price entry with the newest EffectiveFrom seen across suppliers.
        var latestByProduct = new Dictionary<Guid, (decimal Amount, DateTime EffectiveFrom)>();
        var supplied = new HashSet<Guid>();

        foreach (var json in catalogs)
        {
            var entries = JsonSerializer.Deserialize<List<CatalogEntryJson>>(
                json, CatalogJsonOptions) ?? [];

            foreach (var entry in entries)
            {
                if (!wanted.Contains(entry.ProductId)) continue;
                supplied.Add(entry.ProductId);

                var latest = entry.Prices?.MaxBy(p => p.EffectiveFrom);
                if (latest is null) continue;

                if (!latestByProduct.TryGetValue(entry.ProductId, out var current)
                    || latest.EffectiveFrom > current.EffectiveFrom)
                {
                    latestByProduct[entry.ProductId] = (latest.Amount, latest.EffectiveFrom);
                }
            }
        }

        return supplied.ToDictionary(
            id => id,
            id => (latestByProduct.TryGetValue(id, out var p) ? p.Amount : (decimal?)null, true));
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        return excludeId.HasValue
            ? context.ProductReadModels.AnyAsync(p => p.Name == trimmed && p.Id != excludeId.Value, ct)
            : context.ProductReadModels.AnyAsync(p => p.Name == trimmed, ct);
    }

    public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludeId = null, CancellationToken ct = default)
    {
        var trimmed = barcode.Trim();
        return excludeId.HasValue
            ? context.ProductReadModels.AnyAsync(p => p.Barcode == trimmed && p.Id != excludeId.Value, ct)
            : context.ProductReadModels.AnyAsync(p => p.Barcode == trimmed, ct);
    }

    // Supplier catalog JSON shape (mirrors SupplierQueryRepository); case-insensitive read.
    private static readonly JsonSerializerOptions CatalogJsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private sealed record CatalogEntryJson(Guid ProductId, List<CatalogPriceJson>? Prices);
    private sealed record CatalogPriceJson(decimal Amount, string Currency, string Unit, DateTime EffectiveFrom);

    // JSON is written as { "Value": "...", "toUnit": "...", "Factor": ... } — mixed casing.
    // Use explicit [JsonPropertyName] attributes so case-sensitive deserialization works correctly.
    private record ConversionJson(
        [property: System.Text.Json.Serialization.JsonPropertyName("Value")]  string Value,
        [property: System.Text.Json.Serialization.JsonPropertyName("toUnit")] string ToUnit,
        decimal Factor,
        [property: System.Text.Json.Serialization.JsonPropertyName("salePrice")] decimal SalePrice = 0m);
}
