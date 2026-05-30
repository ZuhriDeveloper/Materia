namespace Materia.Infrastructure.Persistence.Projections;

public class CustomerAddressReadModel
{
    public Guid    Id         { get; set; }
    public Guid    CustomerId { get; set; }
    public string  Label      { get; set; } = default!;
    public string  Street     { get; set; } = default!;
    public string  City       { get; set; } = default!;
    public string  Province   { get; set; } = default!;
    public string? PostalCode { get; set; }
    // decimal(11,8) → ±0.00000001° ≈ 1 mm precision
    public decimal Latitude   { get; set; }
    public decimal Longitude  { get; set; }
    public bool    IsDefault  { get; set; }
}
