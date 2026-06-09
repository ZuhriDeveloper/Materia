using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class SaleApiClient(HttpClient http)
{
    public Task<PagedSalesDto?> GetSalesAsync(
        int page = 1, int pageSize = 20,
        string? status = null,
        string? customerName = null,
        string? saleType = null,
        string? referenceNo = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var url = $"api/sales?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status))       url += $"&status={status}";
        if (!string.IsNullOrEmpty(customerName)) url += $"&customerName={Uri.EscapeDataString(customerName)}";
        if (!string.IsNullOrEmpty(saleType))     url += $"&saleType={saleType}";
        if (!string.IsNullOrEmpty(referenceNo))  url += $"&referenceNo={Uri.EscapeDataString(referenceNo)}";
        if (from.HasValue) url += $"&from={from.Value:O}";
        if (to.HasValue)   url += $"&to={to.Value:O}";
        return http.GetFromJsonAsync<PagedSalesDto>(url, ct);
    }

    public Task<SaleDto?> GetSaleByIdAsync(Guid id, CancellationToken ct = default)
        => http.GetFromJsonAsync<SaleDto>($"api/sales/{id}", ct);

    public async Task<(Guid? Id, string? Error)> CreateSaleAsync(
        Guid? customerId, string customerName, string saleType,
        Guid? customerAddressId, string? deliveryAddress,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/sales",
            new { customerId, customerName, saleType, customerAddressId, deliveryAddress }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    public async Task<(Guid? ItemId, string? Error)> AddItemAsync(
        Guid saleId, Guid productId, string productName,
        string unitName, decimal quantity, decimal unitPrice,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/sales/{saleId}/items",
            new { productId, productName, unitName, quantity, unitPrice }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var result = await response.Content.ReadFromJsonAsync<ItemIdResponse>(cancellationToken: ct);
        return (result?.ItemId, null);
    }

    public async Task<string?> RemoveItemAsync(
        Guid saleId, Guid itemId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"api/sales/{saleId}/items/{itemId}", ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<(SaleDto? Sale, string? Error)> CheckoutAsync(
        Guid saleId, decimal paidAmount, string paymentMethod,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/sales/{saleId}/checkout",
            new { paidAmount, paymentMethod }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var sale = await response.Content.ReadFromJsonAsync<SaleDto>(cancellationToken: ct);
        return (sale, null);
    }

    public async Task<string?> CancelAsync(
        Guid saleId, string reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/sales/{saleId}/cancel",
            new { reason }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<(FinalizeSaleResult? Result, string? Error)> FinalizeAsync(
        Guid? customerId, string? customerName,
        IReadOnlyList<FinalizeSaleItemInput> items, bool isDeliveryRequired,
        decimal discount = 0m, bool taxEnabled = false,
        decimal? amountPaid = null, PaymentMethod paymentMethod = PaymentMethod.Cash,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/sales/finalize",
            new
            {
                customerId, customerName, items, isDeliveryRequired,
                discount, taxEnabled, amountPaid, paymentMethod,
            }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var result = await response.Content.ReadFromJsonAsync<FinalizeSaleResult>(cancellationToken: ct);
        return (result, null);
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
    private record ItemIdResponse(Guid ItemId);
    private record ErrorBody(string? Message, List<string>? Errors);
}
