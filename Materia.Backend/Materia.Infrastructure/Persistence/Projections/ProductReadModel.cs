namespace Materia.Infrastructure.Persistence.Projections;

public class ProductReadModel
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string BaseUnit { get; set; } = default!;
    public decimal SalePrice { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>JSON array of { fromUnit, toUnit, factor } objects.</summary>
    public string UnitConversionsJson { get; set; } = "[]";

    /// <summary>JSON array of category IDs assigned to this product.</summary>
    public string CategoryIdsJson { get; set; } = "[]";
}
