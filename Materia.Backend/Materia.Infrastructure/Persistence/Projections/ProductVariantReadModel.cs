namespace Materia.Infrastructure.Persistence.Projections;

/// <summary>
/// Read model for a product's color variants. A dedicated table (rather than JSON on
/// <see cref="ProductReadModel"/>) gives POS an indexed, unique barcode lookup for
/// scan-to-cart across all products.
/// </summary>
public class ProductVariantReadModel
{
    public Guid Id { get; set; }              // VariantId
    public Guid StoreId { get; set; }
    public Guid ProductId { get; set; }
    public string ColorName { get; set; } = default!;
    public string? ColorCode { get; set; }
    public string? Barcode { get; set; }
    public decimal? PriceOverride { get; set; }
    public bool IsActive { get; set; }
}
