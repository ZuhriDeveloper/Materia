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

public class PurchaseOrderRepository(AppDbContext context, ICurrentStore currentStore) : IPurchaseOrderRepository
{
    private const string AggregateType = "PurchaseOrder";

    public async Task<PurchaseOrder?> GetByIdAsync(PurchaseOrderId id, CancellationToken ct = default)
    {
        var storeId = currentStore.StoreId;
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType && e.StoreId == storeId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        var events = stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData));
        return PurchaseOrder.Reconstitute(events);
    }

    public async Task SaveAsync(PurchaseOrder po, CancellationToken ct = default)
    {
        var newEvents = po.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = po.Version - newEvents.Count;
        var storeId = currentStore.StoreId;

        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                StoreId = storeId,
                AggregateType = AggregateType,
                AggregateId = po.Id.Value,
                Version = baseVersion + i + 1,
                EventType = EventTypeRegistry.GetName(evt),
                EventData = EventSerializer.Serialize(evt),
                OccurredAt = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(po, newEvents, ct);
        await context.SaveChangesAsync(ct);
        po.ClearDomainEvents();
    }

    private async Task UpdateProjectionAsync(
        PurchaseOrder po,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        CancellationToken ct)
    {
        var projection = await context.PurchaseOrderReadModels.FindAsync([po.Id.Value], ct);

        if (projection is null)
        {
            var supplierName = await context.SupplierReadModels
                .Where(s => s.Id == po.SupplierId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            projection = new PurchaseOrderReadModel
            {
                Id = po.Id.Value,
                StoreId = currentStore.StoreId,
                SupplierId = po.SupplierId.Value,
                SupplierName = supplierName,
                CreatedBy = newEvents.OfType<PurchaseOrderCreated>().FirstOrDefault()?.CreatedBy ?? string.Empty,
                CreatedAt = po.CreatedAt,
                PaymentTermValue = po.PaymentTerm?.Value,
                PaymentTermUnit = po.PaymentTerm?.Unit.ToString(),
            };
            context.PurchaseOrderReadModels.Add(projection);
        }

        projection.Status = po.Status.ToString();
        projection.ReceivedAt = po.ReceivedAt;
        // Due date (jatuh tempo) anchors on the goods-received date once fully received.
        projection.PaymentDueDate = po is { PaymentTerm: { } term, ReceivedAt: { } receivedAt }
            ? term.DueDateFrom(receivedAt)
            : null;
        projection.LinesJson = JsonSerializer.Serialize(
            po.Lines.Select(l => new
            {
                productId = l.ProductId.Value,
                orderedQty = l.OrderedQty,
                receivedQty = l.ReceivedQty,
                returnedQty = l.ReturnedQty,
                unitCost = l.UnitCost,
                listUnitCost = l.ListUnitCost,
                discounts = l.Discounts,
                unit = l.Unit,
            }));
    }
}
