using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class PurchaseOrderApiClient(HttpClient http)
{
    public Task<List<PurchaseOrderDto>?> GetAllAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<PurchaseOrderDto>>("api/purchase-orders", ct);

    public Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => http.GetFromJsonAsync<PurchaseOrderDto>($"api/purchase-orders/{id}", ct);

    public async Task<(Guid? Id, string? Error)> CreateAsync(
        Guid supplierId, IReadOnlyList<CreatePoLineInput> lines, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/purchase-orders",
            new { supplierId, lines }, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));
        var result = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        return (result?.Id, null);
    }

    public async Task<string?> ConfirmAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/purchase-orders/{id}/confirm", null, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> ReceiveAsync(
        Guid id, IReadOnlyList<ReceivePoLineInput> lines, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/purchase-orders/{id}/receive",
            new { lines }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> CancelAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/purchase-orders/{id}/cancel",
            new { reason }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<(ScannedInvoiceDto? Result, string? Error)> ScanInvoiceAsync(
        Stream imageStream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(imageStream);
        content.Add(fileContent, "file", fileName);

        var response = await http.PostAsync("api/purchase-orders/scan-invoice", content, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<ScannedInvoiceDto>(cancellationToken: ct);
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
    private record ErrorBody(string? Message, List<string>? Errors);
}

public record ScannedInvoiceDto(
    string? SupplierName,
    bool SupplierFound,
    Guid? MatchedSupplierId,
    IReadOnlyList<ScannedLineItemDto> Lines);

public record ScannedLineItemDto(
    string ProductName,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    bool ProductFound,
    Guid? MatchedProductId);
