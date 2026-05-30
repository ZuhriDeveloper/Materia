using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class InventoryApiClient(HttpClient http)
{
    // ── Products ──────────────────────────────────────────────────────────────

    public Task<PagedProductsDto?> GetProductsAsync(
        int page = 1, int pageSize = 20, bool? isActive = null, CancellationToken ct = default)
    {
        var url = $"api/products?page={page}&pageSize={pageSize}";
        if (isActive.HasValue) url += $"&isActive={isActive.Value}";
        return http.GetFromJsonAsync<PagedProductsDto>(url, ct);
    }

    public Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
        => http.GetFromJsonAsync<ProductDto>($"api/products/{id}", ct);

    public async Task<(Guid? Id, string? Error)> CreateProductAsync(
        string name, string? description, string baseUnit, List<Guid>? categoryIds = null, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/products",
            new { name, description, baseUnit, categoryIds }, ct);

        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    public async Task<string?> UpdateProductAsync(
        Guid id, string name, string? description, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/products/{id}",
            new { name, description }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> SetProductStatusAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/products/{id}/status",
            new { isActive }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> AssignCategoryAsync(Guid productId, Guid categoryId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/products/{productId}/categories/{categoryId}", null, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> RemoveCategoryAsync(Guid productId, Guid categoryId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"api/products/{productId}/categories/{categoryId}", ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> AddUnitConversionAsync(
        Guid productId, string toUnit, decimal factor, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/products/{productId}/unit-conversions",
            new { toUnit, factor }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> RemoveUnitConversionAsync(
        Guid productId, string toUnit, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync(
            $"api/products/{productId}/unit-conversions/{Uri.EscapeDataString(toUnit)}", ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    // ── Stock ─────────────────────────────────────────────────────────────────

    public Task<StockDto?> GetStockByProductIdAsync(Guid productId, CancellationToken ct = default)
        => http.GetFromJsonAsync<StockDto>($"api/products/{productId}/stock", ct);

    public async Task<string?> AdjustStockAsync(
        Guid productId, decimal delta, string? reason = null, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            $"api/products/{productId}/stock/adjust", new { delta, reason }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    // ── Units ─────────────────────────────────────────────────────────────────

    public Task<List<UnitDto>?> GetUnitsAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<UnitDto>>("api/units", ct);

    public async Task<(Guid? Id, string? Error)> CreateUnitAsync(
        string name, string? symbol, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/units", new { name, symbol }, ct);

        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    public async Task<string?> UpdateUnitAsync(
        Guid id, string name, string? symbol, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/units/{id}", new { name, symbol }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> SetUnitStatusAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/units/{id}/status", new { isActive }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    // ── Categories ────────────────────────────────────────────────────────────

    public Task<List<CategoryDto>?> GetCategoriesAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<CategoryDto>>("api/categories", ct);

    public async Task<string?> UpdateCategoryAsync(
        Guid id, string name, string? description, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/categories/{id}",
            new { name, description }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> SetCategoryStatusAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/categories/{id}/status",
            new { isActive }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<(Guid? Id, string? Error)> CreateCategoryAsync(
        string name, string? description, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/categories",
            new { name, description }, ct);

        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            return body?.Message ?? body?.Errors?.FirstOrDefault() ?? response.ReasonPhrase ?? "Unknown error";
        }
        catch
        {
            return response.ReasonPhrase ?? "Unknown error";
        }
    }

    private record IdResponse(Guid Id);
    private record ErrorBody(string? Message, List<string>? Errors);
}
