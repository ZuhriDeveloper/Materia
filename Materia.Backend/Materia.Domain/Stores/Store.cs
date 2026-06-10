using Materia.Domain.Common;
using Materia.Domain.Stores.Events;

namespace Materia.Domain.Stores;

/// <summary>
/// A store (toko) — the multi-tenant root. Every transactional record in the
/// system is scoped to a <see cref="StoreId"/>. The store aggregate itself is the
/// tenant boundary; its own events are persisted under its own id.
/// </summary>
public class Store : AggregateRoot<StoreId>
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public bool IsActive { get; private set; }

    // ── Store parameters (optional, set after registration) ──────────────────
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public decimal? MaxDeliveryDistanceKm { get; private set; }

    private Store() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Store Register(string name, string code, string registeredBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Store name cannot be empty.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Store code cannot be empty.");

        var store = new Store();
        store.Raise(new StoreRegistered(
            StoreId.New(),
            name.Trim(),
            code.Trim(),
            registeredBy,
            DateTime.UtcNow));
        return store;
    }

    public static Store Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var store = new Store();
        store.Load(events);
        return store;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void Rename(string name, string renamedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Store name cannot be empty.");

        if (Name != name.Trim())
            Raise(new StoreRenamed(Id, name.Trim(), renamedBy, DateTime.UtcNow));
    }

    public void Deactivate(string deactivatedBy)
    {
        if (!IsActive)
            throw new DomainException("Store is already inactive.");

        Raise(new StoreDeactivated(Id, deactivatedBy, DateTime.UtcNow));
    }

    public void Activate(string activatedBy)
    {
        if (IsActive)
            throw new DomainException("Store is already active.");

        Raise(new StoreActivated(Id, activatedBy, DateTime.UtcNow));
    }

    public void UpdateParameters(string address, string phone, decimal maxDeliveryDistanceKm, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Store address cannot be empty.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("Store phone cannot be empty.");
        if (maxDeliveryDistanceKm <= 0)
            throw new DomainException("Store maximum delivery distance must be greater than zero.");

        Raise(new StoreParametersUpdated(
            Id,
            address.Trim(),
            phone.Trim(),
            maxDeliveryDistanceKm,
            updatedBy,
            DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case StoreRegistered e:
                Id = e.StoreId;
                Name = e.Name;
                Code = e.Code;
                IsActive = true;
                break;

            case StoreRenamed e:
                Name = e.Name;
                break;

            case StoreDeactivated:
                IsActive = false;
                break;

            case StoreActivated:
                IsActive = true;
                break;

            case StoreParametersUpdated e:
                Address = e.Address;
                Phone = e.Phone;
                MaxDeliveryDistanceKm = e.MaxDeliveryDistanceKm;
                break;
        }
    }
}
