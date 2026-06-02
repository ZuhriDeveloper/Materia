using System.Text.Json;
using Materia.Application.Contracts.Inventory;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class ProductRepository(AppDbContext context, IProductSearchCache searchCache) : IProductRepository
{
    private const string AggregateType = "Product";

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
    {
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        var events = stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData));
        return Product.Reconstitute(events);
    }

    public async Task SaveAsync(Product product, CancellationToken ct = default)
    {
        var newEvents = product.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = product.Version - newEvents.Count;

        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                AggregateType = AggregateType,
                AggregateId = product.Id.Value,
                Version = baseVersion + i + 1,
                EventType = EventTypeRegistry.GetName(evt),
                EventData = EventSerializer.Serialize(evt),
                OccurredAt = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(product, newEvents, ct);
        await context.SaveChangesAsync(ct);
        product.ClearDomainEvents();

        // Product catalog changed — drop the cached autocomplete catalog.
        await searchCache.InvalidateAsync(ct);
    }

    private async Task UpdateProjectionAsync(
        Product product,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        CancellationToken ct)
    {
        var projection = await context.ProductReadModels.FindAsync([product.Id.Value], ct);

        if (projection is null)
        {
            var created = newEvents.OfType<ProductCreated>().FirstOrDefault();
            projection = new ProductReadModel
            {
                Id = product.Id.Value,
                BaseUnit = product.BaseUnit.Value,
                CreatedBy = created?.CreatedBy ?? string.Empty,
                CreatedAt = created?.OccurredAt ?? DateTime.UtcNow,
            };
            context.ProductReadModels.Add(projection);
        }

        // Always reflect latest aggregate state
        projection.Name = product.Name;
        projection.Description = product.Description;
        projection.SalePrice = product.SalePrice;
        projection.Barcode = product.Barcode;
        projection.IsActive = product.IsActive;
        projection.UnitConversionsJson = JsonSerializer.Serialize(
            product.UnitConversions.Select(c => new { c.FromUnit.Value, toUnit = c.ToUnit.Value, c.Factor, salePrice = c.SalePrice }));
        projection.CategoryIdsJson = JsonSerializer.Serialize(
            product.CategoryIds.Select(id => id.Value));

        if (newEvents.Any(e => e is not ProductCreated))
        {
            projection.UpdatedBy = newEvents.Last() switch
            {
                ProductNameUpdated e => e.UpdatedBy,
                ProductDescriptionUpdated e => e.UpdatedBy,
                ProductSalePriceChanged e => e.ChangedBy,
                ProductBarcodeChanged e => e.ChangedBy,
                ProductDeactivated e => e.DeactivatedBy,
                ProductActivated e => e.ActivatedBy,
                ProductCategoryAssigned e => e.UpdatedBy,
                ProductCategoryRemoved e => e.UpdatedBy,
                ProductUnitConversionAdded e => e.UpdatedBy,
                ProductUnitConversionRemoved e => e.UpdatedBy,
                _ => projection.UpdatedBy,
            };
            projection.UpdatedAt = newEvents.Last().OccurredAt;
        }
    }
}
