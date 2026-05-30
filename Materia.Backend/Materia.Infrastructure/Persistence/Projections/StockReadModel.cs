namespace Materia.Infrastructure.Persistence.Projections;

public class StockReadModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public DateTime? LastAdjustedAt { get; set; }
    public string? LastAdjustedBy { get; set; }
}
