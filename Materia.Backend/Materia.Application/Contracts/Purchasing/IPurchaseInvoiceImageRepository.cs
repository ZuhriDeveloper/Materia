namespace Materia.Application.Contracts.Purchasing;

public interface IPurchaseInvoiceImageRepository
{
    Task SaveAsync(
        Guid purchaseOrderId, byte[] content, string contentType, string fileName,
        string uploadedBy, CancellationToken ct = default);

    Task<InvoiceImageContent?> GetByPurchaseOrderIdAsync(Guid purchaseOrderId, CancellationToken ct = default);
}

public record InvoiceImageContent(byte[] Content, string ContentType, string FileName);
