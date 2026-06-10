using Materia.Application.Contracts.Stores;
using Materia.Domain.Stores;
using Materia.Domain.Stores.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Stores;

/// <summary>
/// Event-sourced repository for the <see cref="Store"/> aggregate. Unlike the tenant
/// aggregates, a store SELF-OWNS its events: every stored event and the read-model row
/// carry the store's own id, so the non-null StoreId column holds even though the
/// SuperAdmin creating the store has no store scope of their own.
/// </summary>
public class StoreRepository(AppDbContext context) : IStoreRepository
{
    private const string AggregateType = "Store";

    public async Task<Store?> GetByIdAsync(StoreId id, CancellationToken ct = default)
    {
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        return Store.Reconstitute(
            stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData)));
    }

    public async Task SaveAsync(Store store, CancellationToken ct = default)
    {
        var newEvents = store.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = store.Version - newEvents.Count;
        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                StoreId       = store.Id.Value,   // self-owned
                AggregateType = AggregateType,
                AggregateId   = store.Id.Value,
                Version       = baseVersion + i + 1,
                EventType     = EventTypeRegistry.GetName(evt),
                EventData     = EventSerializer.Serialize(evt),
                OccurredAt    = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(store, newEvents, ct);
        await context.SaveChangesAsync(ct);
        store.ClearDomainEvents();
    }

    private async Task UpdateProjectionAsync(
        Store store,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        CancellationToken ct)
    {
        var projection = await context.StoreReadModels.FindAsync([store.Id.Value], ct);

        if (projection is null)
        {
            var created = newEvents.OfType<StoreRegistered>().First();
            projection = new StoreReadModel
            {
                Id        = store.Id.Value,
                CreatedBy = created.RegisteredBy,
                CreatedAt = created.OccurredAt,
            };
            context.StoreReadModels.Add(projection);
        }

        projection.Name                 = store.Name;
        projection.Code                 = store.Code;
        projection.IsActive             = store.IsActive;
        projection.Address              = store.Address;
        projection.Phone                = store.Phone;
        projection.MaxDeliveryDistanceKm = store.MaxDeliveryDistanceKm;
    }
}
