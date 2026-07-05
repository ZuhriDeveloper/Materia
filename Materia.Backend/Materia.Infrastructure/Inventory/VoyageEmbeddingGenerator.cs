using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Materia.Application.Contracts.Inventory;

namespace Materia.Infrastructure.Inventory;

/// <summary>
/// <see cref="IEmbeddingGenerator"/> backed by the Voyage AI embeddings API. The base address
/// and bearer key are configured on the injected typed <see cref="HttpClient"/> in DI. Claude
/// has no embeddings endpoint, so this is an intentionally separate provider from the LLM.
/// </summary>
public class VoyageEmbeddingGenerator(HttpClient http) : IEmbeddingGenerator
{
    public int Dimensions => EmbeddingModel.Dimensions;

    public async Task<float[]> EmbedAsync(string text, EmbeddingPurpose purpose, CancellationToken ct = default)
        => (await EmbedBatchAsync([text], purpose, ct))[0];

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, EmbeddingPurpose purpose, CancellationToken ct = default)
    {
        if (texts.Count == 0) return [];

        var request = new VoyageRequest(
            Input: texts,
            Model: EmbeddingModel.Model,
            // Voyage tunes document vs. query vectors differently; using the right type lifts recall.
            InputType: purpose == EmbeddingPurpose.Query ? "query" : "document",
            OutputDimension: EmbeddingModel.Dimensions);

        using var response = await http.PostAsJsonAsync("v1/embeddings", request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<VoyageResponse>(ct)
            ?? throw new InvalidOperationException("Voyage returned an empty embeddings response.");

        // Align with the input order: Voyage returns each embedding tagged with its input index.
        return payload.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToList();
    }

    private sealed record VoyageRequest(
        [property: JsonPropertyName("input")]            IReadOnlyList<string> Input,
        [property: JsonPropertyName("model")]            string Model,
        [property: JsonPropertyName("input_type")]       string InputType,
        [property: JsonPropertyName("output_dimension")] int OutputDimension);

    private sealed record VoyageResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<VoyageEmbedding> Data);

    private sealed record VoyageEmbedding(
        [property: JsonPropertyName("embedding")] float[] Embedding,
        [property: JsonPropertyName("index")]     int Index);
}
