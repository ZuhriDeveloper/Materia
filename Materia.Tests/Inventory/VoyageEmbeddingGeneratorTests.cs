using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Materia.Application.Contracts.Inventory;
using Materia.Infrastructure.Inventory;

namespace Materia.Tests.Inventory;

public class VoyageEmbeddingGeneratorTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }

    private static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static VoyageEmbeddingGenerator MakeGenerator(StubHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("https://api.voyageai.com/") });

    [Fact]
    public async Task EmbedAsync_PostsExpectedRequest_AndReturnsVector()
    {
        var handler = new StubHandler(_ => Json("""
            {"data":[{"embedding":[0.1,0.2,0.3],"index":0}]}
            """));
        var generator = MakeGenerator(handler);

        var vector = await generator.EmbedAsync("semen", EmbeddingPurpose.Query);

        vector.Should().Equal(0.1f, 0.2f, 0.3f);
        handler.LastRequest!.RequestUri!.AbsoluteUri
            .Should().Be("https://api.voyageai.com/v1/embeddings");

        using var doc = JsonDocument.Parse(handler.LastBody!);
        var root = doc.RootElement;
        root.GetProperty("model").GetString().Should().Be("voyage-3.5");
        root.GetProperty("input_type").GetString().Should().Be("query");
        root.GetProperty("output_dimension").GetInt32().Should().Be(1024);
        root.GetProperty("input")[0].GetString().Should().Be("semen");
    }

    [Fact]
    public async Task EmbedBatchAsync_RealignsResultsToInputOrder_ByIndex()
    {
        // Voyage may return embeddings out of order; each carries its input index.
        var handler = new StubHandler(_ => Json("""
            {"data":[{"embedding":[2.0],"index":1},{"embedding":[1.0],"index":0}]}
            """));
        var generator = MakeGenerator(handler);

        var vectors = await generator.EmbedBatchAsync(["a", "b"], EmbeddingPurpose.Document);

        vectors.Should().HaveCount(2);
        vectors[0].Should().Equal(1.0f);
        vectors[1].Should().Equal(2.0f);

        using var doc = JsonDocument.Parse(handler.LastBody!);
        doc.RootElement.GetProperty("input_type").GetString().Should().Be("document");
    }

    [Fact]
    public async Task EmbedBatchAsync_Empty_DoesNotCallApi()
    {
        var called = false;
        var handler = new StubHandler(_ => { called = true; return Json("{}"); });
        var generator = MakeGenerator(handler);

        var vectors = await generator.EmbedBatchAsync([], EmbeddingPurpose.Document);

        vectors.Should().BeEmpty();
        called.Should().BeFalse("an empty input batch must not cost an API call");
    }
}
