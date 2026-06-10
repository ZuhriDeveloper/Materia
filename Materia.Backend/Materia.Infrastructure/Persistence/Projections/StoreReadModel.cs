namespace Materia.Infrastructure.Persistence.Projections;

/// <summary>
/// Read model for the store registry. This is platform-level data, deliberately
/// EXCLUDED from the per-store global query filter so a SuperAdmin can list every store.
/// </summary>
public class StoreReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    // ── Store parameters (set via UpdateParameters command) ──────────────────
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal? MaxDeliveryDistanceKm { get; set; }
}
