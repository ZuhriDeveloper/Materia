using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Materia.Application.Contracts.Inventory;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Materia.Infrastructure.Inventory;

/// <summary>
/// Keeps the product-embedding index in step with the catalog, out-of-band from the write path.
/// Each cycle it (re)embeds products whose composed searchable text is new or changed (detected via
/// <see cref="ProductEmbeddingReadModel.SourceHash"/>) and drops embeddings for products that became
/// inactive or were removed. Running here — not inside ProductRepository.SaveAsync — keeps catalog
/// writes off the external embeddings API's latency and failure path; the trade-off is a short
/// indexing lag before a newly created product becomes searchable. The first cycle backfills
/// everything (no embeddings exist yet).
/// </summary>
public class ProductEmbeddingIndexer(
    IServiceScopeFactory scopeFactory,
    ILogger<ProductEmbeddingIndexer> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
    private const int BatchSize = 100; // Voyage accepts up to 128 inputs per request.

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A failed cycle (e.g. Voyage key missing/unreachable) must not crash the host;
                // the next tick retries. Search simply serves whatever is already indexed.
                logger.LogError(ex, "Product embedding indexing cycle failed; retrying next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var embeddings = scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator>();

        // Background scope has no current store; read across all stores explicitly.
        var categoryNames = await db.CategoryReadModels.IgnoreQueryFilters()
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var products = await db.ProductReadModels.IgnoreQueryFilters()
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        var existing = await db.ProductEmbeddingReadModels.ToDictionaryAsync(e => e.ProductId, ct);

        // Drop embeddings whose product is gone or no longer active.
        var activeIds = products.Select(p => p.Id).ToHashSet();
        var orphaned = existing.Values.Where(e => !activeIds.Contains(e.ProductId)).ToList();
        if (orphaned.Count > 0)
            db.ProductEmbeddingReadModels.RemoveRange(orphaned);

        // Products whose searchable text is new or changed since last embedded.
        var pending = new List<(ProductReadModel Product, string Text, string Hash)>();
        foreach (var p in products)
        {
            var text = ComposeText(p, categoryNames);
            var hash = Hash(text);
            if (!existing.TryGetValue(p.Id, out var row) || row.SourceHash != hash)
                pending.Add((p, text, hash));
        }

        if (pending.Count == 0)
        {
            if (orphaned.Count > 0) await db.SaveChangesAsync(ct);
            return;
        }

        foreach (var chunk in pending.Chunk(BatchSize))
        {
            var vectors = await embeddings.EmbedBatchAsync(
                chunk.Select(c => c.Text).ToList(), EmbeddingPurpose.Document, ct);

            for (var i = 0; i < chunk.Length; i++)
            {
                var (product, _, hash) = chunk[i];
                if (!existing.TryGetValue(product.Id, out var row))
                {
                    row = new ProductEmbeddingReadModel { ProductId = product.Id };
                    db.ProductEmbeddingReadModels.Add(row);
                    existing[product.Id] = row;
                }

                row.StoreId = product.StoreId;
                row.Embedding = new Vector(vectors[i]);
                row.SourceHash = hash;
                row.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "Product embedding index updated: {Indexed} (re)embedded, {Removed} removed.",
            pending.Count, orphaned.Count);
    }

    /// <summary>
    /// The self-contained "chunk" embedded per product. Indonesian labels (Kategori/Satuan) match
    /// the catalog's domain language so query and document vectors share vocabulary.
    /// </summary>
    private static string ComposeText(ProductReadModel p, IReadOnlyDictionary<Guid, string> categoryNames)
    {
        var categories = string.Join(", ", ParseGuids(p.CategoryIdsJson)
            .Where(categoryNames.ContainsKey)
            .Select(id => categoryNames[id]));

        var sb = new StringBuilder();
        sb.Append(p.Name).Append('.');
        if (!string.IsNullOrWhiteSpace(p.Description))
            sb.Append(' ').Append(p.Description!.Trim()).Append('.');
        if (!string.IsNullOrWhiteSpace(categories))
            sb.Append(" Kategori: ").Append(categories).Append('.');
        sb.Append(" Satuan: ").Append(p.BaseUnit).Append('.');
        return sb.ToString();
    }

    private static string Hash(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static List<Guid> ParseGuids(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<Guid>>(json) ?? []; }
        catch { return []; }
    }
}
