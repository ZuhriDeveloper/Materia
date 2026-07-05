namespace Materia.Application.Contracts.Inventory;

/// <summary>
/// Whether the text is being embedded for storage/indexing (a catalog product) or as a
/// live search query. Some embedding providers (e.g. Voyage) accept an input type that
/// asymmetrically tunes document vs. query vectors to improve retrieval quality.
/// </summary>
public enum EmbeddingPurpose
{
    Document,
    Query,
}

/// <summary>
/// Turns text into a dense vector for semantic similarity search. Implemented in
/// Infrastructure against an external embeddings provider (Voyage AI). Claude has no
/// embeddings API, so this is a deliberately separate port from the LLM integration.
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>The fixed dimensionality every vector this generator produces will have.</summary>
    int Dimensions { get; }

    /// <summary>Embeds a single piece of text.</summary>
    Task<float[]> EmbedAsync(string text, EmbeddingPurpose purpose, CancellationToken ct = default);

    /// <summary>
    /// Embeds many texts in one provider call. Order is preserved: result[i] is the vector
    /// for texts[i]. Used by the background indexer to amortise latency and cost.
    /// </summary>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, EmbeddingPurpose purpose, CancellationToken ct = default);
}
