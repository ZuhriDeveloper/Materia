using System.Text.Json;
using Materia.Application.Contracts.Purchasing;
using Materia.Application.Contracts.Stores;
using Materia.Domain.Purchasing;
using Materia.Domain.Purchasing.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Purchasing;

public class SupplierRepository(AppDbContext context, ICurrentStore currentStore) : ISupplierRepository
{
    private const string AggregateType = "Supplier";

    public async Task<Supplier?> GetByIdAsync(SupplierId id, CancellationToken ct = default)
    {
        var storeId = currentStore.StoreId;
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType && e.StoreId == storeId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        var events = stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData));
        return Supplier.Reconstitute(events);
    }

    public async Task SaveAsync(Supplier supplier, CancellationToken ct = default)
    {
        var newEvents = supplier.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = supplier.Version - newEvents.Count;
        var storeId = currentStore.StoreId;

        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                StoreId = storeId,
                AggregateType = AggregateType,
                AggregateId = supplier.Id.Value,
                Version = baseVersion + i + 1,
                EventType = EventTypeRegistry.GetName(evt),
                EventData = EventSerializer.Serialize(evt),
                OccurredAt = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(supplier, newEvents, ct);
        await context.SaveChangesAsync(ct);
        supplier.ClearDomainEvents();
    }

    private async Task UpdateProjectionAsync(
        Supplier supplier,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        CancellationToken ct)
    {
        var projection = await context.SupplierReadModels.FindAsync([supplier.Id.Value], ct);

        if (projection is null)
        {
            var created = newEvents.OfType<SupplierRegistered>().FirstOrDefault();
            projection = new SupplierReadModel
            {
                Id = supplier.Id.Value,
                StoreId = currentStore.StoreId,
                CreatedBy = created?.CreatedBy ?? string.Empty,
                CreatedAt = created?.OccurredAt ?? DateTime.UtcNow,
            };
            context.SupplierReadModels.Add(projection);
        }

        projection.Name = supplier.Name;
        projection.ContactPhone = supplier.ContactPhone;
        projection.IsActive = supplier.IsActive;
        projection.CatalogJson = JsonSerializer.Serialize(
            supplier.Catalog.Values.Select(entry => new
            {
                productId = entry.ProductId.Value,
                prices = entry.PriceHistory.Select(p => new
                {
                    amount = p.Amount,
                    currency = p.Currency,
                    unit = p.Unit,
                    effectiveFrom = p.EffectiveFrom,
                }),
            }));

        if (newEvents.Any(e => e is not SupplierRegistered))
        {
            projection.UpdatedBy = newEvents.Last() switch
            {
                SupplierUpdated e => e.UpdatedBy,
                SupplierPurchasePriceSet e => e.UpdatedBy,
                SupplierActivated e => e.ActivatedBy,
                SupplierDeactivated e => e.DeactivatedBy,
                _ => projection.UpdatedBy,
            };
            projection.UpdatedAt = newEvents.Last().OccurredAt;
        }
    }
}
