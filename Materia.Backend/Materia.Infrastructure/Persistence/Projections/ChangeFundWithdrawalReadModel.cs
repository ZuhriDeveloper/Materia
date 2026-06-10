namespace Materia.Infrastructure.Persistence.Projections;

/// <summary>
/// Read model for change fund withdrawals — the Admin-only compensating entries that
/// correct erroneous deposits. Balance = SUM(deposits) − SUM(withdrawals). Doubles as
/// the idempotency store via the unique per-store index on <see cref="IdempotencyKey"/>.
/// Rebuildable from the <c>ChangeFundWithdrawn</c> event stream.
/// </summary>
public class ChangeFundWithdrawalReadModel
{
    public Guid     Id             { get; set; }
    public Guid     StoreId        { get; set; }
    public decimal  Amount         { get; set; }
    public string   Reason         { get; set; } = default!;
    public string   RecordedBy     { get; set; } = default!;
    public DateTime RecordedAt     { get; set; }

    /// <summary>Client-supplied de-duplication token. Unique per store.</summary>
    public Guid     IdempotencyKey { get; set; }
}
