using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;

namespace Materia.Application.Queries.Purchasing;

public class ScanPurchaseInvoiceQueryHandler(
    IInvoiceOcrService ocrService,
    ISupplierQueryRepository supplierRepo,
    IProductQueryRepository productRepo)
{
    public async Task<ScannedInvoiceResult> HandleAsync(
        byte[] imageBytes, string mediaType, CancellationToken ct = default)
    {
        var ocr = await ocrService.ScanAsync(imageBytes, mediaType, ct);

        // Match supplier
        var suppliers = await supplierRepo.GetAllAsync(activeOnly: true, ct);
        SupplierDto? matchedSupplier = null;
        if (ocr.SupplierName is not null)
        {
            matchedSupplier = suppliers.FirstOrDefault(s =>
                s.Name.Equals(ocr.SupplierName, StringComparison.OrdinalIgnoreCase))
                ?? suppliers.FirstOrDefault(s =>
                    s.Name.Contains(ocr.SupplierName, StringComparison.OrdinalIgnoreCase)
                    || ocr.SupplierName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
        }

        // Match each line item to a product
        var lines = new List<ScannedLineResult>();
        foreach (var item in ocr.LineItems)
        {
            var paged = await productRepo.GetPagedAsync(1, 5, isActive: true, search: item.ProductName, ct: ct);
            var match = paged.Items.FirstOrDefault(p =>
                p.Name.Equals(item.ProductName, StringComparison.OrdinalIgnoreCase))
                ?? paged.Items.FirstOrDefault();

            lines.Add(new ScannedLineResult(
                ProductName: item.ProductName,
                Quantity: item.Quantity,
                Unit: item.Unit,
                UnitPrice: item.UnitPrice,
                ProductFound: match is not null,
                MatchedProductId: match?.Id));
        }

        return new ScannedInvoiceResult(
            SupplierName: ocr.SupplierName,
            SupplierFound: matchedSupplier is not null,
            MatchedSupplierId: matchedSupplier?.Id,
            Lines: lines);
    }
}

public record ScannedInvoiceResult(
    string? SupplierName,
    bool SupplierFound,
    Guid? MatchedSupplierId,
    IReadOnlyList<ScannedLineResult> Lines);

public record ScannedLineResult(
    string ProductName,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    bool ProductFound,
    Guid? MatchedProductId);
