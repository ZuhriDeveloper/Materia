using System.Text.Json;
using Materia.Application.Contracts.Purchasing;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Purchasing;

/// <summary>
/// Infrastructure implementation of <see cref="ILatestPurchasePriceRepository"/>.
/// Queries the supplier read-model (catalog JSON) and returns the lowest unit cost
/// among all active suppliers who have a purchase price for the given product.
/// <para>
/// "Latest" = the entry with the highest <c>EffectiveFrom</c> date that is &lt;= UtcNow.
/// If multiple suppliers carry the product, the minimum (best) cost is returned so that
/// the COGS snapshot reflects the most competitive known price.
/// </para>
/// </summary>
public sealed class LatestPurchasePriceRepository(AppDbContext context) : ILatestPurchasePriceRepository
{
    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<decimal?> GetLatestCostAsync(Guid productId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var activeSuppliers = await context.SupplierReadModels
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.CatalogJson })
            .ToListAsync(ct);

        decimal? best = null;

        foreach (var row in activeSuppliers)
        {
            var catalog = DeserializeCatalog(row.CatalogJson);
            var entry   = catalog.FirstOrDefault(e => e.ProductId == productId);
            if (entry is null) continue;

            // Pick the price with the highest EffectiveFrom that is on or before now
            var latest = entry.Prices
                .Where(p => p.EffectiveFrom <= now)
                .MaxBy(p => p.EffectiveFrom);

            if (latest is null) continue;

            best = best is null ? latest.Amount : Math.Min(best.Value, latest.Amount);
        }

        return best;
    }

    private static List<CatalogEntryJson> DeserializeCatalog(string json)
        => JsonSerializer.Deserialize<List<CatalogEntryJson>>(json, _jsonOptions) ?? [];

    private sealed record CatalogEntryJson(Guid ProductId, List<PriceJson> Prices);
    private sealed record PriceJson(decimal Amount, string Currency, string Unit, DateTime EffectiveFrom);
}
