using Materia.Application.Contracts.Financials;
using Materia.Application.Contracts.Stores;
using Materia.Domain.Financials;
using Materia.Domain.Financials.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Materia.Infrastructure.Financials;

public class PettyCashRepository(AppDbContext context, ICurrentStore currentStore) : IPettyCashRepository
{
    private const string AggregateType = "PettyCashExpense";

    public async Task<PettyCashExpense?> GetByIdAsync(
        PettyCashExpenseId id, CancellationToken ct = default)
    {
        var storeId = currentStore.StoreId;
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType && e.StoreId == storeId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (stored.Count == 0) return null;

        var events = stored.Select(e => EventSerializer.Deserialize(e.EventType, e.EventData));
        return PettyCashExpense.Reconstitute(events);
    }

    public async Task SaveAsync(PettyCashExpense expense, CancellationToken ct = default)
    {
        var newEvents = expense.DomainEvents;
        if (newEvents.Count == 0) return;

        var baseVersion = expense.Version - newEvents.Count;
        var storeId = currentStore.StoreId;

        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                StoreId       = storeId,
                AggregateType = AggregateType,
                AggregateId   = expense.Id.Value,
                Version       = baseVersion + i + 1,
                EventType     = EventTypeRegistry.GetName(evt),
                EventData     = EventSerializer.Serialize(evt),
                OccurredAt    = evt.OccurredAt,
            });
        }

        UpdateProjection(expense);

        // Translate a duplicate-idempotency-key collision (a concurrent expense with the
        // same client key committed first) into an application-level signal the handler
        // can replay, without leaking the persistence technology into Application.
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsIdempotencyKeyViolation(ex))
        {
            throw new DuplicatePettyCashExpenseException(expense.IdempotencyKey);
        }

        expense.ClearDomainEvents();
    }

    public async Task<Guid?> FindExpenseIdByKeyAsync(
        Guid idempotencyKey, CancellationToken ct = default)
    {
        var row = await context.PettyCashExpenseReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, ct);
        return row?.Id;
    }

    private static bool IsIdempotencyKeyViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg &&
        (pg.ConstraintName?.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase) ?? false);

    private void UpdateProjection(PettyCashExpense expense)
    {
        // Record-only in this increment: a new aggregate maps to a single new read row.
        context.PettyCashExpenseReadModels.Add(new PettyCashExpenseReadModel
        {
            Id             = expense.Id.Value,
            StoreId        = currentStore.StoreId,
            Amount         = expense.Amount,
            Recipient      = expense.Recipient,
            Category       = expense.Category,
            ReasonDetail   = expense.ReasonDetail,
            ReasonText     = expense.Reason,
            Notes          = expense.Notes,
            ReferenceNo    = expense.ReferenceNo,
            RecordedBy     = expense.RecordedBy,
            RecordedAt     = expense.RecordedAt,
            IsVoided       = false,
            IdempotencyKey = expense.IdempotencyKey,
        });
    }
}
