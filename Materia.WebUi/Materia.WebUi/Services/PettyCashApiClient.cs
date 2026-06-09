using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class PettyCashApiClient(HttpClient http)
{
    public Task<PagedPettyCashResult?> GetExpensesAsync(
        int page = 1, int pageSize = 20,
        DateTime? from = null, DateTime? to = null,
        PettyCashCategory? category = null,
        CancellationToken ct = default)
    {
        var url = $"api/pettycash?page={page}&pageSize={pageSize}";
        if (from.HasValue)     url += $"&from={from.Value:yyyy-MM-dd}";
        if (to.HasValue)       url += $"&to={to.Value:yyyy-MM-dd}";
        if (category.HasValue) url += $"&category={(int)category.Value}";
        return http.GetFromJsonAsync<PagedPettyCashResult>(url, ct);
    }

    public Task<PettyCashExpenseResult?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => http.GetFromJsonAsync<PettyCashExpenseResult>($"api/pettycash/{id}", ct);

    public async Task<(Guid? Id, string? Error)> RecordExpenseAsync(
        decimal amount, string recipient, PettyCashCategory category,
        string? reasonDetail, string? notes, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/pettycash",
            new { amount, recipient, category, reasonDetail, notes }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<string> ReadErrorAsync(HttpResponseMessage r)
    {
        try
        {
            var body = await r.Content.ReadFromJsonAsync<ErrorBody>();
            return body?.Message ?? body?.Errors?.FirstOrDefault() ?? r.ReasonPhrase ?? "Unknown error";
        }
        catch { return r.ReasonPhrase ?? "Unknown error"; }
    }

    private record IdResponse(Guid Id);
    private record ErrorBody(string? Message, List<string>? Errors);
}
