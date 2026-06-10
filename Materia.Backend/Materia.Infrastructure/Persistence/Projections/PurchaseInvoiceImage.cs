namespace Materia.Infrastructure.Persistence.Projections;

/// <summary>
/// Stores the original invoice image/PDF that a purchase order was imported from.
/// This is a plain attachment table (not event-sourced) linked to the PO by id.
/// </summary>
public class PurchaseInvoiceImage
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public byte[] Content { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = default!;
}
