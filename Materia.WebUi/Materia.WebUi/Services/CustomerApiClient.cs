using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class CustomerApiClient(HttpClient http)
{
    // ── Customers ─────────────────────────────────────────────────────────────

    public Task<PagedCustomersDto?> GetCustomersAsync(
        int page = 1, int pageSize = 20,
        string? search = null, bool? isActive = null,
        CancellationToken ct = default)
    {
        var url = $"api/customers?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        if (isActive.HasValue)                  url += $"&isActive={isActive.Value}";
        return http.GetFromJsonAsync<PagedCustomersDto>(url, ct);
    }

    public Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default)
        => http.GetFromJsonAsync<CustomerDto>($"api/customers/{id}", ct);

    public Task<List<NearbyCustomerDto>?> GetNearbyAsync(
        decimal latitude, decimal longitude, double radiusKm = 20,
        CancellationToken ct = default)
        => http.GetFromJsonAsync<List<NearbyCustomerDto>>(
            $"api/customers/nearby?latitude={latitude}&longitude={longitude}&radiusKm={radiusKm}", ct);

    public async Task<(Guid? Id, string? Error)> CreateCustomerAsync(
        string name, string phone, string? email, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/customers",
            new { name, phone, email }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    public async Task<string?> UpdateCustomerAsync(
        Guid id, string name, string phone, string? email, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/customers/{id}",
            new { name, phone, email }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> SetCustomerStatusAsync(
        Guid id, bool isActive, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/customers/{id}/status",
            new { isActive }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    // ── Addresses ─────────────────────────────────────────────────────────────

    public async Task<(Guid? AddressId, string? Error)> AddAddressAsync(
        Guid customerId, string label, string street, string city,
        string province, string? postalCode, decimal latitude, decimal longitude,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/customers/{customerId}/addresses",
            new { label, street, city, province, postalCode, latitude, longitude }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var result = await response.Content.ReadFromJsonAsync<AddressIdResponse>(cancellationToken: ct);
        return (result?.AddressId, null);
    }

    public async Task<string?> UpdateAddressAsync(
        Guid customerId, Guid addressId,
        string label, string street, string city,
        string province, string? postalCode, decimal latitude, decimal longitude,
        CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(
            $"api/customers/{customerId}/addresses/{addressId}",
            new { label, street, city, province, postalCode, latitude, longitude }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> RemoveAddressAsync(
        Guid customerId, Guid addressId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync(
            $"api/customers/{customerId}/addresses/{addressId}", ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> SetDefaultAddressAsync(
        Guid customerId, Guid addressId, CancellationToken ct = default)
    {
        var response = await http.PatchAsync(
            $"api/customers/{customerId}/addresses/{addressId}/default", null, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
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
    private record AddressIdResponse(Guid AddressId);
    private record ErrorBody(string? Message, List<string>? Errors);
}
