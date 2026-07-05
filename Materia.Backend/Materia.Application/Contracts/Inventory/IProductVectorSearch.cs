using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Inventory;

/// <summary>
/// Nearest-neighbour search over product embeddings (pgvector). Returns the closest
/// active products to a query vector, ordered most-similar first, each carrying a
/// cosine similarity score in [0,1] (1 = identical direction). Implemented in
/// Infrastructure; the database applies the ORDER BY + LIMIT so the vector index is used.
/// </summary>
public interface IProductVectorSearch
{
    Task<IReadOnlyList<SemanticProductMatch>> SearchAsync(
        float[] queryEmbedding, int limit, CancellationToken ct = default);
}
