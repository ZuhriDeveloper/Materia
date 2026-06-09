using Materia.Domain.Financials;

namespace Materia.Infrastructure.Persistence.Projections;

public class PettyCashExpenseReadModel
{
    public Guid              Id           { get; set; }
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
}
