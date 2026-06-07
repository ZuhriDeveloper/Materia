using System.Text.Json;
using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Materia.Infrastructure.Inventory;

/// <summary>
/// Cache-aside product catalog for PoS autocomplete. Reads serve from Redis; on a cold
/// miss the catalog is loaded once from the read model and written back. If Redis is
/// unreachable, it falls back to the database so the cashier flow never breaks.
/// </summary>
public class RedisProductSearchCache(
    IConnectionMultiplexer redis,
    IProductQueryRepository products,
    ILogger<RedisProductSearchCache> logger) : IProductSearchCache
{
    private const string CacheKey = "products:catalog";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ProductSearchResult>> SearchAsync(
        string term, int limit, CancellationToken ct = default)
    {
        var catalog = await GetCatalogAsync(ct);

        var trimmed = term?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            return catalog.OrderBy(p => p.Name).Take(limit).ToList();

        return catalog
            .Where(p => p.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                     || p.Sku.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                     || (p.Barcode is not null && p.Barcode.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                     || (p.Variants ?? []).Any(v =>
                            v.ColorName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                         || (v.Barcode is not null && v.Barcode.Contains(trimmed, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(p => p.Barcode == trimmed
                                 || (p.Variants ?? []).Any(v => v.Barcode == trimmed))   // exact barcode scan wins
            .ThenByDescending(p => p.Name.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
            .ThenBy(p => p.Name)
            .Take(limit)
            .ToList();
    }

    public async Task InvalidateAsync(CancellationToken ct = default)
    {
        try
        {
            await redis.GetDatabase().KeyDeleteAsync(CacheKey);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis invalidate failed for {Key}; ignoring.", CacheKey);
        }
    }

    // ── Internals ───────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<ProductSearchResult>> GetCatalogAsync(CancellationToken ct)
    {
        var cached = await TryReadFromRedisAsync();
        if (cached is not null)
            return cached;

        var fromDb = await LoadFromDatabaseAsync(ct);
        await TryWriteToRedisAsync(fromDb);
        return fromDb;
    }

    private async Task<IReadOnlyList<ProductSearchResult>?> TryReadFromRedisAsync()
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(CacheKey);
            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<List<ProductSearchResult>>((string)value!, JsonOptions);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis read failed; falling back to database.");
            return null;
        }
    }

    private async Task TryWriteToRedisAsync(IReadOnlyList<ProductSearchResult> catalog)
    {
        try
        {
            var payload = JsonSerializer.Serialize(catalog, JsonOptions);
            await redis.GetDatabase().StringSetAsync(CacheKey, payload, Ttl);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis write failed; catalog not cached.");
        }
    }

    private async Task<IReadOnlyList<ProductSearchResult>> LoadFromDatabaseAsync(CancellationToken ct)
    {
        var paged = await products.GetPagedAsync(1, 1000, isActive: true, ct);
        return paged.Items
            .Select(p => new ProductSearchResult(
                p.Id, p.Name, Sku(p.Id), p.BaseUnit, p.SalePrice, p.Barcode,
                BuildUnits(p), BuildVariants(p)))
            .ToList();
    }

    private static string Sku(Guid productId) => productId.ToString("N")[..8].ToUpperInvariant();

    /// <summary>Base unit (product price) first, then each conversion unit with its own price.</summary>
    private static List<ProductUnitPrice> BuildUnits(ProductDto p)
    {
        var units = new List<ProductUnitPrice> { new(p.BaseUnit, p.SalePrice) };
        units.AddRange(p.UnitConversions.Select(c => new ProductUnitPrice(c.ToUnit, c.SalePrice)));
        return units;
    }

    /// <summary>Active color variants, for PoS color selection and barcode scanning.</summary>
    private static List<ProductVariantSearch> BuildVariants(ProductDto p) =>
        p.ColorVariants
            .Where(v => v.IsActive)
            .Select(v => new ProductVariantSearch(v.Id, v.ColorName, v.ColorCode, v.Barcode, v.EffectivePrice))
            .ToList();
}
