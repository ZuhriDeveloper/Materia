namespace Materia.Application.Services;

/// <summary>
/// Weighted moving-average inventory cost — the COGS basis for stock valuation. Only goods
/// received (purchases) change the average unit cost; issues (sales, manual reductions, supplier
/// returns) leave it untouched and only reduce quantity.
/// </summary>
public static class MovingAverageCost
{
    /// <summary>
    /// The new average unit cost after receiving <paramref name="receivedQty"/> units at
    /// <paramref name="unitCost"/>, given the prior on-hand <paramref name="prevQty"/> and its
    /// average <paramref name="prevAvg"/>. When there is no positive prior quantity (first
    /// receipt, or stock had fallen to zero/negative) the received unit cost becomes the average.
    /// </summary>
    public static decimal AfterReceipt(
        decimal prevQty, decimal prevAvg, decimal receivedQty, decimal unitCost)
    {
        if (receivedQty <= 0m) return prevAvg;
        if (prevQty <= 0m) return unitCost;
        return (prevQty * prevAvg + receivedQty * unitCost) / (prevQty + receivedQty);
    }
}
