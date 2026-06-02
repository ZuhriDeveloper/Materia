using Materia.Domain.Inventory;

namespace Materia.Domain.Purchasing;

public sealed class SupplierCatalogEntry
{
    public ProductId ProductId { get; }
    public IReadOnlyList<PurchasePrice> PriceHistory => _priceHistory.AsReadOnly();

    private readonly List<PurchasePrice> _priceHistory = [];

    public SupplierCatalogEntry(ProductId productId) => ProductId = productId;

    internal void AddPrice(PurchasePrice price) => _priceHistory.Add(price);

    public PurchasePrice? LatestPrice =>
        _priceHistory.Count == 0 ? null : _priceHistory.MaxBy(p => p.EffectiveFrom);
}
