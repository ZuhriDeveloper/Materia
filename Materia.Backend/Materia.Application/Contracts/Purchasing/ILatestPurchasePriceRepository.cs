namespace Materia.Application.Contracts.Purchasing;

/// <summary>
/// Port: retrieves the most recent supplier purchase price for a product.
/// Used by the finalize-sale handler to snapshot a unit cost per sale line at the
/// time of sale (so historical margin calculations remain immutable).
/// <para>
/// "Latest" is defined as the entry with the highest <c>EffectiveFrom</c> date that
/// is at or before the current UTC time. If multiple suppliers carry the same product,
/// the <em>lowest</em> cost among the latest prices per supplier is used (best cost).
/// </para>
/// </summary>
public interface ILatestPurchasePriceRepository
{
    /// <summary>
    /// Returns the best (lowest) latest unit cost for <paramref name="productId"/>,
    /// or <c>null</c> when no supplier has a purchase price on record for that product.
    /// </summary>
    Task<decimal?> GetLatestCostAsync(Guid productId, CancellationToken ct = default);
}
