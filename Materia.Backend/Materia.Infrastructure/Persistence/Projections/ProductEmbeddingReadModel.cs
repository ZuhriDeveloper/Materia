using Pgvector;

namespace Materia.Infrastructure.Persistence.Projections;

/// <summary>
/// The semantic-search index row for a product: one dense vector per product, stored in a
/// pgvector column. Decoupled from the write path — a background indexer (re)builds these
/// out-of-band so catalog writes never wait on the external embeddings API. <see cref="SourceHash"/>
/// is the hash of the composed text that was embedded, letting the indexer skip products
/// whose searchable text hasn't changed.
/// </summary>
public class ProductEmbeddingReadModel
{
    public Guid ProductId { get; set; }
    public Guid StoreId { get; set; }
    public Vector Embedding { get; set; } = default!;
    public string SourceHash { get; set; } = default!;
    public DateTime UpdatedAt { get; set; }
}
