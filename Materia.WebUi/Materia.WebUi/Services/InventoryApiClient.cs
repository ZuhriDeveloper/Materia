using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class InventoryApiClient(HttpClient http)
{
    // ── Products ──────────────────────────────────────────────────────────────

    public Task<PagedProductsDto?> GetProductsAsync(
        int page = 1, int pageSize = 20, bool? isActive = null,
        string? search = null, Guid? categoryId = null,
        CancellationToken ct = default)
    {
        var url = $"api/products?page={page}&pageSize={pageSize}";
        if (isActive.HasValue)
            url += $"&isActive={isActive.Value}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search.Trim())}";
        if (categoryId.HasValue)
            url += $"&categoryId={categoryId.Value}";
        return http.GetFromJsonAsync<PagedProductsDto>(url, ct);
    }

    public Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
        => http.GetFromJsonAsync<ProductDto>($"api/products/{id}", ct);

    /// <summary>Redis-backed product-name autocomplete for the PoS cashier.</summary>
    public async Task<List<ProductSearchDto>> SearchProductsAsync(
        string term, int limit = 8, CancellationToken ct = default)
    {
        var url = $"api/products/search?term={Uri.EscapeDataString(term)}&limit={limit}";
        var results = await http.GetFromJsonAsync<List<ProductSearchDto>>(url, ct);
        return results ?? [];
    }

    public async Task<(Guid? Id, string? Error)> CreateProductAsync(
        string name, string? description, string baseUnit, List<Guid>? categoryIds = null,
        decimal salePrice = 0m, string? barcode = null, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/products",
            new { name, description, baseUnit, categoryIds, salePrice, barcode }, ct);

        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    public async Task<string?> UpdateProductAsync(
        Guid id, string name, string? description,
        decimal salePrice = 0m, string? barcode = null, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/products/{id}",
            new { name, description, salePrice, barcode }, ct);
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
        Guid productId, string toUnit, decimal factor, decimal salePrice = 0m, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/products/{productId}/unit-conversions",
            new { toUnit, factor, salePrice }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> RemoveUnitConversionAsync(
        Guid productId, string toUnit, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync(
            $"api/products/{productId}/unit-conversions/{Uri.EscapeDataString(toUnit)}", ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    // ── Color Variants ──────────────────────────────────────────────────────────

    public async Task<(Guid? VariantId, string? Error)> AddColorVariantAsync(
        Guid productId, string colorName, string? colorCode, string? barcode,
        decimal? priceOverride, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/products/{productId}/color-variants",
            new { colorName, colorCode, barcode, priceOverride }, ct);

        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<VariantIdResponse>(cancellationToken: ct);
        return (result?.VariantId, null);
    }

    public async Task<string?> UpdateColorVariantAsync(
        Guid productId, Guid variantId, string colorName, string? colorCode, string? barcode,
        decimal? priceOverride, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/products/{productId}/color-variants/{variantId}",
            new { colorName, colorCode, barcode, priceOverride }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> RemoveColorVariantAsync(
        Guid productId, Guid variantId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"api/products/{productId}/color-variants/{variantId}", ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> SetColorVariantStatusAsync(
        Guid productId, Guid variantId, bool isActive, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync(
            $"api/products/{productId}/color-variants/{variantId}/status", new { isActive }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    // ── Stock ─────────────────────────────────────────────────────────────────

    public async Task<StockDto?> GetStockByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        // A product with no stock record yet returns 404 — treat that as "no stock".
        var response = await http.GetAsync($"api/products/{productId}/stock", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StockDto>(cancellationToken: ct);
    }

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
    private record VariantIdResponse(Guid VariantId);
    private record ErrorBody(string? Message, List<string>? Errors);
}
