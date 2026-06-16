using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class SupplierApiClient(HttpClient http)
{
    public Task<PagedResult<SupplierDto>?> GetSuppliersAsync(
        string? search     = null,
        bool    activeOnly = false,
        int     page       = 1,
        int     pageSize   = 20,
        CancellationToken ct = default)
    {
        var url = $"api/suppliers?activeOnly={activeOnly}&page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        return http.GetFromJsonAsync<PagedResult<SupplierDto>>(url, ct);
    }

    public Task<SupplierDto?> GetSupplierByIdAsync(Guid id, CancellationToken ct = default)
        => http.GetFromJsonAsync<SupplierDto>($"api/suppliers/{id}", ct);

    public async Task<(Guid? Id, string? Error)> RegisterAsync(
        string name, string? contactPhone, string? description,
        string? salesmanName, string? salesmanPhone, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/suppliers",
            new { name, contactPhone, description, salesmanName, salesmanPhone }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    public async Task<string?> UpdateAsync(
        Guid id, string name, string? contactPhone, string? description,
        string? salesmanName, string? salesmanPhone, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/suppliers/{id}",
            new { name, contactPhone, description, salesmanName, salesmanPhone }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> SetStatusAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/suppliers/{id}/status",
            new { isActive }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> SetPriceAsync(
        Guid supplierId, Guid productId, decimal amount,
        string currency, string unit, DateTime? effectiveFrom,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/suppliers/{supplierId}/prices",
            new { productId, amount, currency, unit, effectiveFrom }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

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
