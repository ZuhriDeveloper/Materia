namespace Materia.Infrastructure.Persistence.Projections;

public class SupplierReadModel
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Name { get; set; } = default!;
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
    public string CatalogJson { get; set; } = "[]";
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
