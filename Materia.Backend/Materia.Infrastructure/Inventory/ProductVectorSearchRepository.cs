using System.Text.Json;
using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

/// <summary>
/// Nearest-neighbour product search over pgvector. Ranks active products by cosine distance to
/// the query vector (the HNSW <c>vector_cosine_ops</c> index serves this ORDER BY), then enriches
/// the top matches with the same stock / price / category derivations as the keyword catalog
/// search so both endpoints return identically-shaped data.
/// </summary>
public class ProductVectorSearchRepository(AppDbContext context) : IProductVectorSearch
{
    public async Task<IReadOnlyList<SemanticProductMatch>> SearchAsync(
        float[] queryEmbedding, int limit, CancellationToken ct = default)
    {
        var vector = new Vector(queryEmbedding);

        // `<=>` cosine distance in [0,2]; cosine similarity = 1 - distance.
        var ranked = await (
            from emb in context.ProductEmbeddingReadModels
            join p in context.ProductReadModels on emb.ProductId equals p.Id
            where p.IsActive
            orderby emb.Embedding.CosineDistance(vector)
            select new RankedRow(
                p.Id, p.Name, p.Description, p.BaseUnit, p.CategoryIdsJson,
                emb.Embedding.CosineDistance(vector)))
            .Take(limit)
            .ToListAsync(ct);

        if (ranked.Count == 0) return [];

        var ids = ranked.Select(r => r.Id).ToList();

        var stocks = await context.StockReadModels
            .Where(s => ids.Contains(s.ProductId))
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        // Average price from sale history (best-effort; 0 if never sold) — mirrors CatalogController.
        var avgPrices = await context.SaleItemReadModels
            .Where(i => ids.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Price = g.Average(i => i.UnitPrice) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Price, ct);

        var categoryIds = ranked.SelectMany(r => ParseGuids(r.CategoryIdsJson)).Distinct().ToList();
        var categoryNames = categoryIds.Count > 0
            ? await context.CategoryReadModels
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct)
            : new Dictionary<Guid, string>();

        return ranked.Select(r =>
        {
            stocks.TryGetValue(r.Id, out var qty);
            avgPrices.TryGetValue(r.Id, out var price);

            var firstCatId = ParseGuids(r.CategoryIdsJson).FirstOrDefault();
            var category = firstCatId != Guid.Empty && categoryNames.TryGetValue(firstCatId, out var cn)
                ? cn : "";

            return new SemanticProductMatch(
                r.Id, r.Name, r.Description, r.BaseUnit,
                (decimal)Math.Round(price, 0), qty, category,
                Score: 1.0 - r.Distance);
        }).ToList();
    }

    private sealed record RankedRow(
        Guid Id, string Name, string? Description, string BaseUnit, string CategoryIdsJson, double Distance);

    private static List<Guid> ParseGuids(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<Guid>>(json) ?? []; }
        catch { return []; }
    }
}
