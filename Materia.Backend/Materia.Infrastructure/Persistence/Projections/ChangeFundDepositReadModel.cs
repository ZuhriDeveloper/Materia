using Materia.Domain.Financials;

namespace Materia.Infrastructure.Persistence.Projections;

/// <summary>
/// Read model for change fund deposits. Doubles as the idempotency store:
/// <see cref="IdempotencyKey"/> has a unique index per store so a retried or concurrent
/// duplicate submission can never persist a second deposit (the ledger is append-only,
/// so a duplicate would permanently inflate the balance).
/// Rebuildable from the <c>ChangeFundDeposited</c> event stream.
/// </summary>
public class ChangeFundDepositReadModel
{
    public Guid             Id                { get; set; }
    public Guid             StoreId           { get; set; }
    public decimal          Amount            { get; set; }
    public ChangeFundSource Source            { get; set; }
    public string?          SourceReferenceNo { get; set; }
    public string?          Notes             { get; set; }
    public string           RecordedBy        { get; set; } = default!;
    public DateTime         RecordedAt        { get; set; }

    /// <summary>Client-supplied de-duplication token. Unique per store.</summary>
    public Guid             IdempotencyKey    { get; set; }
}
