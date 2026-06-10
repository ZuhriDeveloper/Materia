using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class StoreSettingsApiClient(HttpClient http)
{
    // ── Profile ───────────────────────────────────────────────────────────────

    public Task<StoreProfileDto?> GetProfileAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<StoreProfileDto>("api/store/profile", ct);

    public async Task<List<string>?> UpdateProfileAsync(
        string address, string phone, double maxDeliveryDistanceKm,
        CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync("api/store/profile",
            new { address, phone, maxDeliveryDistanceKm }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorsAsync(response);
    }

    // ── Logo ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Uploads a store logo via multipart/form-data.
    /// Returns (fileName, null) on success; (null, errors) on failure.
    /// </summary>
    public async Task<(string? FileName, List<string>? Errors)> UploadLogoAsync(
        Stream fileStream, string fileName, string contentType,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        var response = await http.PostAsync("api/store/profile/logo", content, ct);

        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorsAsync(response));

        var result = await response.Content
            .ReadFromJsonAsync<LogoUploadResponse>(cancellationToken: ct);
        return (result?.FileName, null);
    }

    /// <summary>
    /// Fetches the logo bytes and returns them as a base64 data URL
    /// suitable for use in an &lt;img&gt; src attribute.
    /// Returns null if the store has no logo (404).
    /// </summary>
    public async Task<string?> GetLogoDataUrlAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/store/profile/logo", ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var mime  = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<List<string>> ReadErrorsAsync(HttpResponseMessage r)
    {
        // 413 from nginx/Kestrel has no JSON body
        if (r.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
            return ["File terlalu besar (maks. 2 MB)."];

        try
        {
            var body = await r.Content.ReadFromJsonAsync<ErrorBody>();
            if (body?.Errors is { Count: > 0 })
                return body.Errors;
            if (body?.Message is not null)
                return [body.Message];
        }
        catch { }
        return [r.ReasonPhrase ?? "Unknown error"];
    }

    private record LogoUploadResponse(string FileName);
    private record ErrorBody(string? Message, List<string>? Errors);
}
