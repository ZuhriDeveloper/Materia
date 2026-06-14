using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing.Events;

namespace Materia.Domain.Purchasing;

public sealed class Supplier : AggregateRoot<SupplierId>
{
    public string Name { get; private set; } = default!;
    public string? ContactPhone { get; private set; }
    public string? Description { get; private set; }
    public string? SalesmanName { get; private set; }
    public string? SalesmanPhone { get; private set; }
    public bool IsActive { get; private set; }

    private readonly Dictionary<Guid, SupplierCatalogEntry> _catalog = [];
    public IReadOnlyDictionary<Guid, SupplierCatalogEntry> Catalog => _catalog;

    private Supplier() { }

    // ── Factories ─────────────────────────────────────────────────────────────

    public static Supplier Register(
        string name,
        string? contactPhone,
        string? description,
        string? salesmanName,
        string? salesmanPhone,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Supplier name is required.");

        var supplier = new Supplier();
        supplier.Raise(new SupplierRegistered(
            SupplierId.New(), name, contactPhone, description, salesmanName, salesmanPhone,
            createdBy, DateTime.UtcNow));
        return supplier;
    }

    public static Supplier Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var s = new Supplier();
        s.Load(events);
        return s;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void Update(
        string name,
        string? contactPhone,
        string? description,
        string? salesmanName,
        string? salesmanPhone,
        string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Supplier name is required.");

        Raise(new SupplierUpdated(
            Id, name, contactPhone, description, salesmanName, salesmanPhone,
            updatedBy, DateTime.UtcNow));
    }

    public void SetPurchasePrice(ProductId productId, PurchasePrice price, string updatedBy)
    {
        if (!IsActive)
            throw new DomainException("Cannot update catalog of an inactive supplier.");

        Raise(new SupplierPurchasePriceSet(
            Id, productId, price.Amount, price.Currency, price.Unit,
            price.EffectiveFrom, updatedBy, DateTime.UtcNow));
    }

    public void Activate(string activatedBy)
    {
        if (IsActive) throw new DomainException("Supplier is already active.");
        Raise(new SupplierActivated(Id, activatedBy, DateTime.UtcNow));
    }

    public void Deactivate(string deactivatedBy)
    {
        if (!IsActive) throw new DomainException("Supplier is already inactive.");
        Raise(new SupplierDeactivated(Id, deactivatedBy, DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case SupplierRegistered e:
                Id = e.SupplierId;
                Name = e.Name;
                ContactPhone = e.ContactPhone;
                Description = e.Description;
                SalesmanName = e.SalesmanName;
                SalesmanPhone = e.SalesmanPhone;
                IsActive = true;
                break;

            case SupplierUpdated e:
                Name = e.Name;
                ContactPhone = e.ContactPhone;
                Description = e.Description;
                SalesmanName = e.SalesmanName;
                SalesmanPhone = e.SalesmanPhone;
                break;

            case SupplierPurchasePriceSet e:
                if (!_catalog.TryGetValue(e.ProductId.Value, out var entry))
                {
                    entry = new SupplierCatalogEntry(e.ProductId);
                    _catalog[e.ProductId.Value] = entry;
                }
                entry.AddPrice(new PurchasePrice(e.Amount, e.Currency, e.Unit, e.EffectiveFrom));
                break;

            case SupplierActivated:
                IsActive = true;
                break;

            case SupplierDeactivated:
                IsActive = false;
                break;
        }
    }
}
