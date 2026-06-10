using System.Text.Json;
using Materia.Application.Contracts.Customers;
using Materia.Application.Contracts.Stores;
using Materia.Domain.Customers;
using Materia.Domain.Customers.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Materia.Infrastructure.Customers;

public class CustomerRepository(AppDbContext context, ICurrentStore currentStore) : ICustomerRepository
{
    private const string AggregateType = "Customer";

    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
    {
        var storeId = currentStore.StoreId;
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType && e.StoreId == storeId)
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
        var storeId = currentStore.StoreId;
        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                StoreId       = storeId,
                AggregateType = AggregateType,
                AggregateId   = customer.Id.Value,
                Version       = baseVersion + i + 1,
                EventType     = EventTypeRegistry.GetName(evt),
                EventData     = EventSerializer.Serialize(evt),
                OccurredAt    = evt.OccurredAt,
            });
        }

        await UpdateProjectionAsync(customer, newEvents, ct);

        // Translate a duplicate-idempotency-key collision (a concurrent payment with the same
        // client key committed first) into an application-level signal the handler can replay,
        // without leaking the persistence technology into the Application layer.
        var paymentKey = newEvents.OfType<ReceivablePaymentRecorded>()
            .Select(e => (Guid?)e.IdempotencyKey)
            .FirstOrDefault();
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (paymentKey is not null && IsUniqueViolation(ex, "IdempotencyKey"))
        {
            throw new DuplicateReceivablePaymentException(paymentKey.Value);
        }

        customer.ClearDomainEvents();
    }

    public async Task<StoredReceivablePayment?> FindReceivablePaymentByKeyAsync(
        Guid idempotencyKey, CancellationToken ct = default)
    {
        var row = await context.ReceivablePaymentReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);

        if (row is null) return null;

        var allocations =
            JsonSerializer.Deserialize<List<StoredReceivableAllocation>>(row.AllocationsJson)
            ?? [];

        return new StoredReceivablePayment(row.Id, row.NewBalance, allocations);
    }

    private static bool IsUniqueViolation(DbUpdateException ex, string constraintFragment) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg &&
        (pg.ConstraintName?.Contains(constraintFragment, StringComparison.OrdinalIgnoreCase) ?? false);

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
                StoreId   = currentStore.StoreId,
                CreatedBy = created.CreatedBy,
                CreatedAt = created.OccurredAt,
            };
            context.CustomerReadModels.Add(projection);
        }

        projection.Name            = customer.Name;
        projection.Phone           = customer.Phone.Value;
        projection.Email           = customer.Email;
        projection.IsActive        = customer.IsActive;
        projection.OutstandingDebt = customer.OutstandingDebt;

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
                CustomerDebtIncurred e          => e.IncurredBy,
                ReceivablePaymentRecorded e     => e.ReceivedBy,
                _                               => projection.UpdatedBy,
            };
            projection.UpdatedAt = newEvents.Last().OccurredAt;
        }

        // Sync addresses: update in-place, add new rows, delete removed rows.
        // Never Clear() + re-add: EF Core would try to DELETE and INSERT the same PK
        // in one SaveChanges when an address is updated, causing a concurrency exception.
        MergeAddresses(projection, customer);

        // Append a payment-history row per collection. The unique index on IdempotencyKey
        // makes this insert the idempotency enforcement point.
        foreach (var pay in newEvents.OfType<ReceivablePaymentRecorded>())
        {
            context.ReceivablePaymentReadModels.Add(new ReceivablePaymentReadModel
            {
                Id              = pay.PaymentId,
                StoreId         = currentStore.StoreId,
                IdempotencyKey  = pay.IdempotencyKey,
                CustomerId      = customer.Id.Value,
                Amount          = pay.Amount,
                NewBalance      = pay.NewBalance,
                Method          = pay.Method,
                Notes           = pay.Notes,
                ReceivedBy      = pay.ReceivedBy,
                RecordedAt      = pay.OccurredAt,
                AllocationsJson = JsonSerializer.Serialize(
                    pay.Allocations.Select(a => new StoredReceivableAllocation(
                        a.SaleId, a.ReferenceNo, a.AppliedAmount, a.RemainingAfter))),
            });
        }
    }

    private void MergeAddresses(CustomerReadModel projection, Customer customer)
    {
        var tracked    = projection.Addresses.ToDictionary(a => a.Id);
        var aggregateIds = customer.Addresses.Select(a => a.Id.Value).ToHashSet();

        // Remove rows that are no longer in the aggregate
        foreach (var stale in tracked.Values.Where(a => !aggregateIds.Contains(a.Id)).ToList())
            context.CustomerAddressReadModels.Remove(stale);

        // Update existing rows in-place; insert new rows
        foreach (var addr in customer.Addresses)
        {
            if (tracked.TryGetValue(addr.Id.Value, out var row))
            {
                row.Label       = addr.Label;
                row.Street      = addr.Street;
                row.Subdistrict = addr.Subdistrict;
                row.District    = addr.District;
                row.City        = addr.City;
                row.Province    = addr.Province;
                row.PostalCode  = addr.PostalCode;
                row.Latitude    = addr.Coordinates?.Latitude;
                row.Longitude   = addr.Coordinates?.Longitude;
                row.IsDefault   = addr.IsDefault;
            }
            else
            {
                context.CustomerAddressReadModels.Add(new CustomerAddressReadModel
                {
                    Id          = addr.Id.Value,
                    StoreId     = currentStore.StoreId,
                    CustomerId  = customer.Id.Value,
                    Label       = addr.Label,
                    Street      = addr.Street,
                    Subdistrict = addr.Subdistrict,
                    District    = addr.District,
                    City        = addr.City,
                    Province    = addr.Province,
                    PostalCode  = addr.PostalCode,
                    Latitude    = addr.Coordinates?.Latitude,
                    Longitude   = addr.Coordinates?.Longitude,
                    IsDefault   = addr.IsDefault,
                });
            }
        }
    }
}
