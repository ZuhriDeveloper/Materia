namespace Materia.Infrastructure.Persistence.Projections;

public class PurchaseOrderReadModel
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string LinesJson { get; set; } = "[]";
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
}
