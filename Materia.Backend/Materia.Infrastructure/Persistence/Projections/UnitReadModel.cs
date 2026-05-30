namespace Materia.Infrastructure.Persistence.Projections;

public class UnitReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Symbol { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
