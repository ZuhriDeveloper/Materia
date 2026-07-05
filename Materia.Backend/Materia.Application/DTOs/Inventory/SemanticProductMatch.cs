namespace Materia.Application.DTOs.Inventory;

/// <summary>
/// A single product returned by semantic (vector) search, with everything Renovin needs
/// to ground a Claude answer: identity, the human-facing fields, and the similarity
/// <see cref="Score"/> (cosine, [0,1]) used both for ranking and for relevance filtering.
/// </summary>
public record SemanticProductMatch(
    Guid    Id,
    string  Name,
    string? Description,
    string  Unit,
    decimal Price,
    decimal Stock,
    string  Category,
    double  Score);
