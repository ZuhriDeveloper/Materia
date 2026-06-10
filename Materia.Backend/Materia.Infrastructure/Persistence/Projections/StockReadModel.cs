namespace Materia.Infrastructure.Persistence.Projections;

public class StockReadModel
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>The color variant this stock is for, or null for product-level stock.</summary>
    public Guid? VariantId { get; set; }

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public DateTime? LastAdjustedAt { get; set; }
    public string? LastAdjustedBy { get; set; }
}
