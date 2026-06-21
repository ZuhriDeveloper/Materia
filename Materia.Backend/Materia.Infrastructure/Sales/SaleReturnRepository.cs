using Materia.Application.Contracts.Sales;
using Materia.Application.Contracts.Stores;
using Materia.Domain.Sales;
using Materia.Domain.Sales.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Sales;

public class SaleReturnRepository(AppDbContext context, ICurrentStore currentStore) : ISaleReturnRepository
{
    private const string AggregateType = "SaleReturn";

    public async Task<SaleReturn?> GetByIdAsync(SaleReturnId id, CancellationToken ct = default)
    {
        var storeId = currentStore.StoreId;
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType && e.StoreId == storeId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        return SaleReturn.Reconstitute(
            stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData)));
    }

    public async Task SaveAsync(SaleReturn saleReturn, CancellationToken ct = default)
    {
        var newEvents = saleReturn.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = saleReturn.Version - newEvents.Count;
        var storeId = currentStore.StoreId;
        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                StoreId       = storeId,
                AggregateType = AggregateType,
                AggregateId   = saleReturn.Id.Value,
                Version       = baseVersion + i + 1,
                EventType     = EventTypeRegistry.GetName(evt),
                EventData     = EventSerializer.Serialize(evt),
                OccurredAt    = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(saleReturn, newEvents, storeId, ct);
        await context.SaveChangesAsync(ct);
        saleReturn.ClearDomainEvents();
    }

    private async Task UpdateProjectionAsync(
        SaleReturn saleReturn,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        Guid storeId,
        CancellationToken ct)
    {
        // SaleReturn is created in a single event — always upsert header + replace all lines
        var recorded = newEvents.OfType<SaleReturnRecorded>().FirstOrDefault();
        if (recorded is null) return;

        var projection = await context.SaleReturnReadModels
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == saleReturn.Id.Value, ct);

        if (projection is null)
        {
            projection = new SaleReturnReadModel { Id = saleReturn.Id.Value, StoreId = storeId };
            context.SaleReturnReadModels.Add(projection);
        }

        projection.OriginalSaleId      = saleReturn.OriginalSaleId.Value;
        projection.OriginalReferenceNo = saleReturn.OriginalReferenceNo;
        projection.TotalRefundAmount   = saleReturn.TotalRefundAmount;
        projection.Resolution          = saleReturn.Resolution;
        projection.Reason              = saleReturn.Reason;
        projection.ReturnedBy          = saleReturn.ReturnedBy;
        projection.ReturnedAt          = saleReturn.ReturnedAt;

        // Replace lines
        context.SaleReturnLineReadModels.RemoveRange(projection.Lines);
        projection.Lines = saleReturn.Lines.Select(l => new SaleReturnLineReadModel
        {
            Id                 = Guid.NewGuid(),
            SaleReturnId       = saleReturn.Id.Value,
            StoreId            = storeId,
            ProductId          = l.ProductId,
            VariantId          = l.VariantId,
            ProductName        = l.ProductName,
            ColorName          = l.ColorName,
            UnitName           = l.UnitName,
            Quantity           = l.Quantity,
            QuantityInBaseUnit = l.QuantityInBaseUnit,
            UnitPrice          = l.UnitPrice,
            Subtotal           = l.Subtotal,
        }).ToList();
    }
}
