namespace Materia.Application.Contracts.Purchasing;

public interface IInvoiceOcrService
{
    Task<OcrInvoiceScanResult> ScanAsync(byte[] imageBytes, string mediaType, CancellationToken ct = default);
}

public record OcrInvoiceScanResult(string? SupplierName, IReadOnlyList<OcrLineItem> LineItems);

public record OcrLineItem(string ProductName, decimal Quantity, string Unit, decimal UnitPrice);
