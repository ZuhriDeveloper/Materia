namespace Materia.Infrastructure.Persistence.Projections;

public class CustomerAddressReadModel
{
    public Guid    Id         { get; set; }
    public Guid    StoreId    { get; set; }
    public Guid    CustomerId { get; set; }
    public string  Label       { get; set; } = default!;
    public string  Street      { get; set; } = default!;
    public string? Subdistrict { get; set; }   // Kelurahan / Desa
    public string? District    { get; set; }   // Kecamatan
    public string  City        { get; set; } = default!;
    public string  Province    { get; set; } = default!;
    public string? PostalCode  { get; set; }
    // Optional map pin. Null when the address has not been located. decimal(11,8) ≈ 1 mm precision.
    public decimal? Latitude   { get; set; }
    public decimal? Longitude  { get; set; }
    public bool    IsDefault  { get; set; }
}
