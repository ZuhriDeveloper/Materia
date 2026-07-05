using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Inventory;

/// <summary>
/// Semantic product search: find catalog products by meaning rather than literal substring.
/// The query text is embedded, matched against product vectors, then filtered to the
/// closest <see cref="TopK"/> whose similarity clears <see cref="MinScore"/> so weak,
/// off-topic matches never reach the caller (and, downstream, never reach an LLM prompt).
/// </summary>
public record SemanticProductSearchQuery(string Query, int TopK = 5, double MinScore = 0.5);

public class SemanticProductSearchQueryHandler(
    IEmbeddingGenerator embeddings,
    IProductVectorSearch search)
{
    public async Task<IReadOnlyList<SemanticProductMatch>> HandleAsync(
        SemanticProductSearchQuery query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
            return [];

        var topK = Math.Clamp(query.TopK, 1, 20);

        var vector = await embeddings.EmbedAsync(query.Query.Trim(), EmbeddingPurpose.Query, ct);

        var matches = await search.SearchAsync(vector, topK, ct);

        // Relevance threshold: drop anything below MinScore so the result set is "the few
        // genuinely relevant products" rather than "the K least-far rows in the table".
        return matches
            .Where(m => m.Score >= query.MinScore)
            .OrderByDescending(m => m.Score)
            .Take(topK)
            .ToList();
    }
}
