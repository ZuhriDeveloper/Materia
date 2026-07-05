namespace Materia.Infrastructure.Inventory;

/// <summary>
/// The single source of truth for the embedding model and its vector dimensionality.
/// The pgvector column type (<c>vector(Dimensions)</c>) and the Voyage request must agree;
/// changing the model/dimension here requires a migration AND a full re-embed of the catalog.
/// </summary>
internal static class EmbeddingModel
{
    /// <summary>Voyage model id. voyage-3.5 is multilingual (handles Indonesian/English).</summary>
    public const string Model = "voyage-3.5";

    /// <summary>Output dimension requested from Voyage; must equal the pgvector column dimension.</summary>
    public const int Dimensions = 1024;
}
