using Materia.Application.Contracts.Purchasing;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Purchasing;

public class PurchaseInvoiceImageRepository(AppDbContext context) : IPurchaseInvoiceImageRepository
{
    public async Task SaveAsync(
        Guid purchaseOrderId, byte[] content, string contentType, string fileName,
        string uploadedBy, CancellationToken ct = default)
    {
        // One image per PO — replace any existing attachment.
        var existing = await context.PurchaseInvoiceImages
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .ToListAsync(ct);
        if (existing.Count > 0)
            context.PurchaseInvoiceImages.RemoveRange(existing);

        context.PurchaseInvoiceImages.Add(new PurchaseInvoiceImage
        {
            Id              = Guid.NewGuid(),
            PurchaseOrderId = purchaseOrderId,
            FileName        = fileName,
            ContentType     = contentType,
            Content         = content,
            UploadedAt      = DateTime.UtcNow,
            UploadedBy      = uploadedBy,
        });

        await context.SaveChangesAsync(ct);
    }

    public async Task<InvoiceImageContent?> GetByPurchaseOrderIdAsync(
        Guid purchaseOrderId, CancellationToken ct = default)
    {
        var row = await context.PurchaseInvoiceImages
            .AsNoTracking()
            .OrderByDescending(x => x.UploadedAt)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == purchaseOrderId, ct);

        return row is null ? null : new InvoiceImageContent(row.Content, row.ContentType, row.FileName);
    }
}
