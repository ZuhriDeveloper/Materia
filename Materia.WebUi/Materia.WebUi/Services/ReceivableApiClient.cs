using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class ReceivableApiClient(HttpClient http)
{
    public Task<PagedReceivablesResult?> GetOutstandingAsync(
        int page = 1, int pageSize = 20,
        string? search = null,
        CancellationToken ct = default)
    {
        var url = $"api/receivables?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return http.GetFromJsonAsync<PagedReceivablesResult>(url, ct);
    }

    public async Task<(RecordPaymentResult? Result, string? Error)> RecordPaymentAsync(
        Guid customerId, decimal amount, PaymentMethod method,
        string? notes, Guid idempotencyKey,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/receivables/payments",
            new { customerId, amount, method = (int)method, notes, idempotencyKey }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var result = await response.Content.ReadFromJsonAsync<RecordPaymentResult>(cancellationToken: ct);
        return (result, null);
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

    private record ErrorBody(string? Message, List<string>? Errors);
}
