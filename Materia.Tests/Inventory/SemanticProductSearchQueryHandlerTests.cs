using FluentAssertions;
using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;
using Materia.Application.Queries.Inventory;

namespace Materia.Tests.Inventory;

public class SemanticProductSearchQueryHandlerTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    /// <summary>Records the text/purpose it was asked to embed and returns a fixed vector.</summary>
    private sealed class FakeEmbeddingGenerator : IEmbeddingGenerator
    {
        public string? LastText { get; private set; }
        public EmbeddingPurpose? LastPurpose { get; private set; }
        public int Dimensions => 3;

        public Task<float[]> EmbedAsync(string text, EmbeddingPurpose purpose, CancellationToken ct = default)
        {
            LastText = text;
            LastPurpose = purpose;
            return Task.FromResult(new[] { 0.1f, 0.2f, 0.3f });
        }

        public Task<IReadOnlyList<float[]>> EmbedBatchAsync(
            IReadOnlyList<string> texts, EmbeddingPurpose purpose, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a canned, pre-scored candidate set. The DB would apply ORDER BY + LIMIT;
    /// here we hand the handler an unordered set so its own ordering/threshold logic is tested.
    /// </summary>
    private sealed class FakeProductVectorSearch(IReadOnlyList<SemanticProductMatch> results)
        : IProductVectorSearch
    {
        public int? LastLimit { get; private set; }
        public float[]? LastVector { get; private set; }

        public Task<IReadOnlyList<SemanticProductMatch>> SearchAsync(
            float[] queryEmbedding, int limit, CancellationToken ct = default)
        {
            LastVector = queryEmbedding;
            LastLimit = limit;
            return Task.FromResult(results);
        }
    }

    private static SemanticProductMatch Match(string name, double score)
        => new(Guid.NewGuid(), name, null, "pcs", 1000m, 5m, "Cat", score);

    private static SemanticProductSearchQueryHandler MakeHandler(
        IReadOnlyList<SemanticProductMatch> results, FakeEmbeddingGenerator? embeddings = null)
        => new(embeddings ?? new FakeEmbeddingGenerator(), new FakeProductVectorSearch(results));

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmbedsTheQuery_WithQueryPurpose_AndTrimsWhitespace()
    {
        var embeddings = new FakeEmbeddingGenerator();
        var handler = new SemanticProductSearchQueryHandler(
            embeddings, new FakeProductVectorSearch([Match("Semen", 0.9)]));

        await handler.HandleAsync(new SemanticProductSearchQuery("  semen waterproof  "), default);

        embeddings.LastText.Should().Be("semen waterproof");
        embeddings.LastPurpose.Should().Be(EmbeddingPurpose.Query);
    }

    [Fact]
    public async Task FiltersOutMatchesBelowMinScore()
    {
        var results = new[]
        {
            Match("Relevant",   0.82),
            Match("Borderline", 0.40),
            Match("Irrelevant", 0.10),
        };
        var handler = MakeHandler(results);

        var result = await handler.HandleAsync(
            new SemanticProductSearchQuery("anti bocor", TopK: 5, MinScore: 0.5), default);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Relevant");
    }

    [Fact]
    public async Task OrdersByScoreDescending()
    {
        var results = new[]
        {
            Match("Mid",  0.70),
            Match("High", 0.95),
            Match("Low",  0.55),
        };
        var handler = MakeHandler(results);

        var result = await handler.HandleAsync(
            new SemanticProductSearchQuery("cat tembok", MinScore: 0.5), default);

        result.Select(m => m.Name).Should().ContainInOrder("High", "Mid", "Low");
    }

    [Fact]
    public async Task LimitsToTopK_AfterThreshold()
    {
        var results = Enumerable.Range(1, 10)
            .Select(i => Match($"P{i:D2}", 0.90 - i * 0.01))
            .ToArray();
        var handler = MakeHandler(results);

        var result = await handler.HandleAsync(
            new SemanticProductSearchQuery("paku", TopK: 3, MinScore: 0.5), default);

        result.Should().HaveCount(3);
        result.Select(m => m.Name).Should().ContainInOrder("P01", "P02", "P03");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankQuery_ReturnsEmpty_WithoutEmbedding(string query)
    {
        var embeddings = new FakeEmbeddingGenerator();
        var handler = new SemanticProductSearchQueryHandler(
            embeddings, new FakeProductVectorSearch([Match("X", 0.9)]));

        var result = await handler.HandleAsync(new SemanticProductSearchQuery(query), default);

        result.Should().BeEmpty();
        embeddings.LastText.Should().BeNull("a blank query must not cost an embedding call");
    }

    [Fact]
    public async Task ClampsTopK_ToTwenty()
    {
        var search = new FakeProductVectorSearch([]);
        var handler = new SemanticProductSearchQueryHandler(new FakeEmbeddingGenerator(), search);

        await handler.HandleAsync(new SemanticProductSearchQuery("x", TopK: 999), default);

        search.LastLimit.Should().Be(20);
    }
}
