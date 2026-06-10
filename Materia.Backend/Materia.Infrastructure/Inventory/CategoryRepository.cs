using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Stores;
using Materia.Domain.Inventory;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class CategoryRepository(AppDbContext context, ICurrentStore currentStore) : ICategoryRepository
{
    private const string AggregateType = "Category";

    public async Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default)
    {
        var storeId = currentStore.StoreId;
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType && e.StoreId == storeId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        var events = stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData));
        return Category.Reconstitute(events);
    }

    public async Task<bool> ExistsAsync(CategoryId id, CancellationToken ct = default)
        => await context.CategoryReadModels.AnyAsync(c => c.Id == id.Value, ct);

    public async Task SaveAsync(Category category, CancellationToken ct = default)
    {
        var newEvents = category.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = category.Version - newEvents.Count;
        var storeId = currentStore.StoreId;

        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                StoreId = storeId,
                AggregateType = AggregateType,
                AggregateId = category.Id.Value,
                Version = baseVersion + i + 1,
                EventType = EventTypeRegistry.GetName(evt),
                EventData = EventSerializer.Serialize(evt),
                OccurredAt = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(category, newEvents, ct);
        await context.SaveChangesAsync(ct);
        category.ClearDomainEvents();
    }

    private async Task UpdateProjectionAsync(
        Category category,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        CancellationToken ct)
    {
        var projection = await context.CategoryReadModels.FindAsync([category.Id.Value], ct);

        if (projection is null)
        {
            var created = newEvents.OfType<Domain.Inventory.Events.CategoryCreated>().First();
            projection = new CategoryReadModel
            {
                Id = category.Id.Value,
                StoreId = currentStore.StoreId,
                CreatedBy = created.CreatedBy,
                CreatedAt = created.OccurredAt,
                IsActive = true,
            };
            context.CategoryReadModels.Add(projection);
        }

        projection.Name = category.Name;
        projection.Description = category.Description;
        projection.IsActive = category.IsActive;

        if (newEvents.Any(e => e is not Domain.Inventory.Events.CategoryCreated))
        {
            projection.UpdatedBy = newEvents.Last() switch
            {
                Domain.Inventory.Events.CategoryNameUpdated e        => e.UpdatedBy,
                Domain.Inventory.Events.CategoryDescriptionUpdated e => e.UpdatedBy,
                Domain.Inventory.Events.CategoryDeactivated e        => e.DeactivatedBy,
                Domain.Inventory.Events.CategoryActivated e          => e.ActivatedBy,
                _ => projection.UpdatedBy,
            };
            projection.UpdatedAt = newEvents.Last().OccurredAt;
        }
    }
}
