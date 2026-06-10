using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class ChangeFundApiClient(HttpClient http)
{
    public Task<ChangeFundResultDto?> GetAsync(
        int page = 1, int pageSize = 20,
        CancellationToken ct = default)
        => http.GetFromJsonAsync<ChangeFundResultDto>(
            $"api/change-fund?page={page}&pageSize={pageSize}", ct);

    public async Task<(Guid? Id, List<string>? Errors)> RecordDepositAsync(
        decimal amount, string? notes, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/change-fund",
            new { amount, notes }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorsAsync(response));
        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<List<string>> ReadErrorsAsync(HttpResponseMessage r)
    {
        try
        {
            var body = await r.Content.ReadFromJsonAsync<ErrorBody>();
            if (body?.Errors is { Count: > 0 }) return body.Errors;
            if (body?.Message is not null)       return [body.Message];
        }
        catch { }
        return [r.ReasonPhrase ?? "Unknown error"];
    }

    private record IdResponse(Guid Id);
    private record ErrorBody(string? Message, List<string>? Errors);
}
