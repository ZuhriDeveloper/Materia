using Materia.Application.Contracts.Sales;
using Materia.Domain.Sales;
using Materia.Domain.Sales.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Sales;

public class SaleRepository(AppDbContext context) : ISaleRepository
{
    private const string AggregateType = "Sale";

    public async Task<Sale?> GetByIdAsync(SaleId id, CancellationToken ct = default)
    {
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        return Sale.Reconstitute(
            stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData)));
    }

    public async Task SaveAsync(Sale sale, CancellationToken ct = default)
    {
        var newEvents = sale.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = sale.Version - newEvents.Count;
        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                AggregateType = AggregateType,
                AggregateId   = sale.Id.Value,
                Version       = baseVersion + i + 1,
                EventType     = EventTypeRegistry.GetName(evt),
                EventData     = EventSerializer.Serialize(evt),
                OccurredAt    = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(sale, newEvents, ct);
        await context.SaveChangesAsync(ct);
        sale.ClearDomainEvents();
    }

    private async Task UpdateProjectionAsync(
        Sale sale,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        CancellationToken ct)
    {
        var projection = await context.SaleReadModels
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sale.Id.Value, ct);

        if (projection is null)
        {
            var created = newEvents.OfType<SaleCreated>().First();
            projection = new SaleReadModel
            {
                Id          = sale.Id.Value,
                ReferenceNo = created.ReferenceNo,
                CreatedBy   = created.CreatedBy,
                CreatedAt   = created.OccurredAt,
            };
            context.SaleReadModels.Add(projection);
        }

        projection.CustomerId        = sale.CustomerId;
        projection.CustomerName      = sale.CustomerName;
        projection.CustomerAddressId = sale.CustomerAddressId;
        projection.DeliveryAddress   = sale.DeliveryAddress;
        projection.SaleType          = sale.SaleType;
        projection.Status            = sale.Status;
        projection.Subtotal          = sale.Subtotal.Amount;
        projection.GrandTotal        = sale.GrandTotal.Amount;

        if (sale.Payment is not null)
        {
            projection.PaidAmount    = sale.Payment.PaidAmount.Amount;
            projection.Change        = sale.Payment.Change.Amount;
            projection.PaymentMethod = sale.Payment.Method;
            projection.PaidAt        = sale.Payment.PaidAt;
        }

        MergeItems(projection, sale);
    }

    private void MergeItems(SaleReadModel projection, Sale sale)
    {
        var tracked = projection.Items.ToDictionary(i => i.Id);
        var aggIds  = sale.Items.Select(i => i.Id.Value).ToHashSet();

        foreach (var stale in tracked.Values.Where(i => !aggIds.Contains(i.Id)).ToList())
            context.SaleItemReadModels.Remove(stale);

        foreach (var item in sale.Items)
        {
            if (tracked.TryGetValue(item.Id.Value, out var row))
            {
                row.Quantity           = item.Quantity;
                row.QuantityInBaseUnit = item.QuantityInBaseUnit;
                row.UnitPrice          = item.UnitPrice.Amount;
                row.Subtotal           = item.Subtotal.Amount;
            }
            else
            {
                context.SaleItemReadModels.Add(new SaleItemReadModel
                {
                    Id                 = item.Id.Value,
                    SaleId             = sale.Id.Value,
                    ProductId          = item.ProductId,
                    ProductName        = item.ProductName,
                    UnitName           = item.UnitName,
                    Quantity           = item.Quantity,
                    QuantityInBaseUnit = item.QuantityInBaseUnit,
                    UnitPrice          = item.UnitPrice.Amount,
                    Subtotal           = item.Subtotal.Amount,
                });
            }
        }
    }
}
