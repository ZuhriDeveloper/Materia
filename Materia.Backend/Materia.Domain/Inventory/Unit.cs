using Materia.Domain.Common;
using Materia.Domain.Inventory.Events;

namespace Materia.Domain.Inventory;

public class Unit : AggregateRoot<UnitId>
{
    public string Name { get; private set; } = default!;
    public string? Symbol { get; private set; }
    public bool IsActive { get; private set; }

    private Unit() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Unit Create(string name, string? symbol, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Unit name cannot be empty.");

        var unit = new Unit();
        unit.Raise(new UnitCreated(UnitId.New(), name.Trim(), symbol?.Trim(), createdBy, DateTime.UtcNow));
        return unit;
    }

    public static Unit Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var unit = new Unit();
        unit.Load(events);
        return unit;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void Update(string name, string? symbol, string updatedBy)
    {
        if (!IsActive)
            throw new DomainException("Cannot update an inactive unit.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Unit name cannot be empty.");

        var trimmedName = name.Trim();
        var trimmedSymbol = symbol?.Trim();

        if (Name != trimmedName || Symbol != trimmedSymbol)
            Raise(new UnitUpdated(Id, trimmedName, trimmedSymbol, updatedBy, DateTime.UtcNow));
    }

    public void Deactivate(string deactivatedBy)
    {
        if (!IsActive)
            throw new DomainException("Unit is already inactive.");

        Raise(new UnitDeactivated(Id, deactivatedBy, DateTime.UtcNow));
    }

    public void Activate(string activatedBy)
    {
        if (IsActive)
            throw new DomainException("Unit is already active.");

        Raise(new UnitActivated(Id, activatedBy, DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case UnitCreated e:
                Id = e.UnitId;
                Name = e.Name;
                Symbol = e.Symbol;
                IsActive = true;
                break;

            case UnitUpdated e:
                Name = e.Name;
                Symbol = e.Symbol;
                break;

            case UnitDeactivated:
                IsActive = false;
                break;

            case UnitActivated:
                IsActive = true;
                break;
        }
    }
}
