using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class PlatformApiClient(HttpClient http)
{
    // ── Stores ────────────────────────────────────────────────────────────────

    public Task<List<StoreDto>?> GetStoresAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<StoreDto>>("api/platform/stores", ct);

    public async Task<(Guid? Id, List<string>? Errors)> RegisterStoreAsync(
        string name, string code, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/platform/stores",
            new { name, code }, ct);

        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorsAsync(response));

        var result = await response.Content
            .ReadFromJsonAsync<RegisterStoreResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    public async Task<List<string>?> RenameStoreAsync(
        Guid id, string name, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(
            $"api/platform/stores/{id}/name", new { name }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorsAsync(response);
    }

    public async Task<List<string>?> SetStoreStatusAsync(
        Guid id, bool isActive, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync(
            $"api/platform/stores/{id}/status", new { isActive }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorsAsync(response);
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    public async Task<(string? UserId, List<string>? Errors)> RegisterUserAsync(
        string email, string fullName, string password, string role, Guid storeId,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/platform/users",
            new { email, fullName, password, role, storeId }, ct);

        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorsAsync(response));

        var result = await response.Content
            .ReadFromJsonAsync<RegisterUserResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<List<string>> ReadErrorsAsync(HttpResponseMessage r)
    {
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

    private record ErrorBody(string? Message, List<string>? Errors);
}
