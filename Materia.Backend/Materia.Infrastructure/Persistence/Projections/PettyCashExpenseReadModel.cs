using Materia.Domain.Financials;

namespace Materia.Infrastructure.Persistence.Projections;

public class PettyCashExpenseReadModel
{
    public Guid              Id           { get; set; }
    public Guid              StoreId      { get; set; }
    public decimal           Amount       { get; set; }
    public string            Recipient    { get; set; } = default!;
    public PettyCashCategory Category     { get; set; }
    public string?           ReasonDetail { get; set; }

    /// <summary>Denormalised display reason (category label, or the Sebutkan text for Lainnya).</summary>
    public string            ReasonText   { get; set; } = default!;

    public string?           Notes        { get; set; }
    public string            ReferenceNo  { get; set; } = default!;
    public string            RecordedBy   { get; set; } = default!;
    public DateTime          RecordedAt   { get; set; }

    /// <summary>Reserved for a future void/cancel capability; always false in this increment.</summary>
    public bool              IsVoided     { get; set; }

    /// <summary>
    /// Client-supplied de-duplication token; unique per store so a retried submission can
    /// never record the expense twice. Rows from before the key existed were backfilled
    /// with random values by the migration.
    /// </summary>
    public Guid              IdempotencyKey { get; set; }
}
