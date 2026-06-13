namespace Materia.Infrastructure.Persistence.Projections;

public class PurchaseOrderReadModel
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string LinesJson { get; set; } = "[]";
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    // Payment tenor (tempo). Null term = cash / no tempo.
    public int? PaymentTermValue { get; set; }
    public string? PaymentTermUnit { get; set; }
    // Due date (jatuh tempo), set once the goods are fully received (ReceivedAt + term).
    public DateTime? PaymentDueDate { get; set; }
}
