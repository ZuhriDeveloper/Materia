using Materia.Application.Contracts.Inventory;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class UnitRepository(AppDbContext context) : IUnitRepository
{
    private const string AggregateType = "Unit";

    public async Task<Unit?> GetByIdAsync(UnitId id, CancellationToken ct = default)
    {
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        var events = stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData));
        return Unit.Reconstitute(events);
    }

    public Task<bool> ExistsAsync(UnitId id, CancellationToken ct = default)
        => context.UnitReadModels.AnyAsync(u => u.Id == id.Value, ct);

    public async Task SaveAsync(Unit unit, CancellationToken ct = default)
    {
        var newEvents = unit.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = unit.Version - newEvents.Count;

        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                AggregateType = AggregateType,
                AggregateId = unit.Id.Value,
                Version = baseVersion + i + 1,
                EventType = EventTypeRegistry.GetName(evt),
                EventData = EventSerializer.Serialize(evt),
                OccurredAt = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(unit, newEvents, ct);
        await context.SaveChangesAsync(ct);
        unit.ClearDomainEvents();
    }

    private async Task UpdateProjectionAsync(
        Unit unit,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        CancellationToken ct)
    {
        var projection = await context.UnitReadModels.FindAsync([unit.Id.Value], ct);

        if (projection is null)
        {
            var created = newEvents.OfType<UnitCreated>().First();
            projection = new UnitReadModel
            {
                Id = unit.Id.Value,
                CreatedBy = created.CreatedBy,
                CreatedAt = created.OccurredAt,
                IsActive = true,
            };
            context.UnitReadModels.Add(projection);
        }

        projection.Name = unit.Name;
        projection.Symbol = unit.Symbol;
        projection.IsActive = unit.IsActive;

        if (newEvents.Any(e => e is not UnitCreated))
        {
            projection.UpdatedBy = newEvents.Last() switch
            {
                UnitUpdated e     => e.UpdatedBy,
                UnitDeactivated e => e.DeactivatedBy,
                UnitActivated e   => e.ActivatedBy,
                _ => projection.UpdatedBy,
            };
            projection.UpdatedAt = newEvents.Last().OccurredAt;
        }
    }
}
