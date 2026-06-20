namespace Materia.Application.Contracts.Inventory;

/// <summary>
/// One-time maintenance: recompute the moving-average cost projection for existing stock buckets
/// by replaying their purchase receipts. Idempotent — the average is deterministic from the events.
/// </summary>
public interface IStockCostBackfill
{
    /// <summary>Recomputes and persists average cost; returns the number of buckets updated.</summary>
    Task<int> RunAsync(CancellationToken ct = default);
}
