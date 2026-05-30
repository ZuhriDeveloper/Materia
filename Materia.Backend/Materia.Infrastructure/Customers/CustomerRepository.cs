using Materia.Application.Contracts.Customers;
using Materia.Domain.Customers;
using Materia.Domain.Customers.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Customers;

public class CustomerRepository(AppDbContext context) : ICustomerRepository
{
    private const string AggregateType = "Customer";

    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
    {
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        var events = stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData));
        return Customer.Reconstitute(events);
    }

    public async Task SaveAsync(Customer customer, CancellationToken ct = default)
    {
        var newEvents = customer.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = customer.Version - newEvents.Count;
        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                AggregateType = AggregateType,
                AggregateId   = customer.Id.Value,
                Version       = baseVersion + i + 1,
                EventType     = EventTypeRegistry.GetName(evt),
                EventData     = EventSerializer.Serialize(evt),
                OccurredAt    = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(customer, newEvents, ct);
        await context.SaveChangesAsync(ct);
        customer.ClearDomainEvents();
    }

    private async Task UpdateProjectionAsync(
        Customer customer,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents,
        CancellationToken ct)
    {
        var projection = await context.CustomerReadModels
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == customer.Id.Value, ct);

        if (projection is null)
        {
            var created = newEvents.OfType<CustomerCreated>().First();
            projection = new CustomerReadModel
            {
                Id        = customer.Id.Value,
                CreatedBy = created.CreatedBy,
                CreatedAt = created.OccurredAt,
            };
            context.CustomerReadModels.Add(projection);
        }

        projection.Name     = customer.Name;
        projection.Phone    = customer.Phone.Value;
        projection.Email    = customer.Email;
        projection.IsActive = customer.IsActive;

        if (newEvents.Any(e => e is not CustomerCreated))
        {
            projection.UpdatedBy = newEvents.Last() switch
            {
                CustomerUpdated e               => e.UpdatedBy,
                CustomerActivated e             => e.ActivatedBy,
                CustomerDeactivated e           => e.DeactivatedBy,
                CustomerAddressAdded e          => e.UpdatedBy,
                CustomerAddressUpdated e        => e.UpdatedBy,
                CustomerAddressRemoved e        => e.UpdatedBy,
                CustomerDefaultAddressChanged e => e.UpdatedBy,
                _                               => projection.UpdatedBy,
            };
            projection.UpdatedAt = newEvents.Last().OccurredAt;
        }

        // Sync addresses — full replace to keep projection consistent
        projection.Addresses.Clear();
        foreach (var addr in customer.Addresses)
        {
            projection.Addresses.Add(new CustomerAddressReadModel
            {
                Id         = addr.Id.Value,
                CustomerId = customer.Id.Value,
                Label      = addr.Label,
                Street     = addr.Street,
                City       = addr.City,
                Province   = addr.Province,
                PostalCode = addr.PostalCode,
                Latitude   = addr.Coordinates.Latitude,
                Longitude  = addr.Coordinates.Longitude,
                IsDefault  = addr.IsDefault,
            });
        }
    }
}
