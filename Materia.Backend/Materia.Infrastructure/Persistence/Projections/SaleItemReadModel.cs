namespace Materia.Infrastructure.Persistence.Projections;

public class SaleItemReadModel
{
    public Guid    Id                 { get; set; }
    public Guid    SaleId             { get; set; }
    public Guid    ProductId          { get; set; }
    public Guid?   VariantId          { get; set; }
    public string  ProductName        { get; set; } = default!;
    public string? ColorName          { get; set; }
    public string  UnitName           { get; set; } = default!;
    public decimal Quantity           { get; set; }
    public decimal QuantityInBaseUnit { get; set; }
    public decimal UnitPrice          { get; set; }
    public decimal Subtotal           { get; set; }
}
