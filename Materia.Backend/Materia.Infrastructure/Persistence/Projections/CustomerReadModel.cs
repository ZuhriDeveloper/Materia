namespace Materia.Infrastructure.Persistence.Projections;

public class CustomerReadModel
{
    public Guid      Id        { get; set; }
    public string    Name      { get; set; } = default!;
    public string    Phone     { get; set; } = default!;
    public string?   Email     { get; set; }
    public bool      IsActive  { get; set; }
    public string    CreatedBy { get; set; } = default!;
    public DateTime  CreatedAt { get; set; }
    public string?   UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<CustomerAddressReadModel> Addresses { get; set; } = [];
}
