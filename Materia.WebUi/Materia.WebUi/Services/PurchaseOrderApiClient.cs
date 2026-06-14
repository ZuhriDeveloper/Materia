using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class PurchaseOrderApiClient(HttpClient http)
{
    public Task<List<PurchaseOrderDto>?> GetAllAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<PurchaseOrderDto>>("api/purchase-orders", ct);

    public Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => http.GetFromJsonAsync<PurchaseOrderDto>($"api/purchase-orders/{id}", ct);

    public async Task<(Guid? Id, string? Error)> CreateAsync(
        Guid supplierId, IReadOnlyList<CreatePoLineInput> lines,
        int? paymentTermValue = null, PaymentTermUnit? paymentTermUnit = null,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/purchase-orders",
            new { supplierId, lines, paymentTermValue, paymentTermUnit = paymentTermUnit?.ToString() }, ct);
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

    public async Task<string?> ReturnAsync(
        Guid id, IReadOnlyList<ReturnPoLineInput> lines, string reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/purchase-orders/{id}/return",
            new { lines, reason }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> CloseAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/purchase-orders/{id}/close",
            new { reason }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<string?> CancelAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/purchase-orders/{id}/cancel",
            new { reason }, ct);
        return response.IsSuccessStatusCode ? null : await ReadErrorAsync(response);
    }

    public async Task<(ScannedInvoiceDto? Result, string? Error)> ScanInvoiceAsync(
        Stream imageStream, string fileName, string contentType, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(imageStream);
        if (!string.IsNullOrWhiteSpace(contentType))
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        var response = await http.PostAsync("api/purchase-orders/scan-invoice", content, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<ScannedInvoiceDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<string?> UploadInvoiceImageAsync(
        Guid purchaseOrderId, byte[] content, string contentType, string fileName,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(fileContent, "file", fileName);

        var response = await http.PostAsync($"api/purchase-orders/{purchaseOrderId}/invoice-image", form, ct);
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
