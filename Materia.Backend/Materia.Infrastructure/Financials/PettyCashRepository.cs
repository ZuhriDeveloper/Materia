using Materia.Application.Contracts.Financials;
using Materia.Domain.Financials;
using Materia.Domain.Financials.Events;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Financials;

public class PettyCashRepository(AppDbContext context) : IPettyCashRepository
{
    private const string AggregateType = "PettyCashExpense";

    public async Task<PettyCashExpense?> GetByIdAsync(
        PettyCashExpenseId id, CancellationToken ct = default)
    {
        var stored = await context.StoredEvents
            .Where(e => e.AggregateId == id.Value && e.AggregateType == AggregateType)
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

        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                AggregateType = AggregateType,
                AggregateId   = expense.Id.Value,
                Version       = baseVersion + i + 1,
                EventType     = EventTypeRegistry.GetName(evt),
                EventData     = EventSerializer.Serialize(evt),
                OccurredAt    = evt.OccurredAt,
            });
        }

        UpdateProjection(expense);
        await context.SaveChangesAsync(ct);
        expense.ClearDomainEvents();
    }

    private void UpdateProjection(PettyCashExpense expense)
    {
        // Record-only in this increment: a new aggregate maps to a single new read row.
        context.PettyCashExpenseReadModels.Add(new PettyCashExpenseReadModel
        {
            Id           = expense.Id.Value,
            Amount       = expense.Amount,
            Recipient    = expense.Recipient,
            Category     = expense.Category,
            ReasonDetail = expense.ReasonDetail,
            ReasonText   = expense.Reason,
            Notes        = expense.Notes,
            ReferenceNo  = expense.ReferenceNo,
            RecordedBy   = expense.RecordedBy,
            RecordedAt   = expense.RecordedAt,
            IsVoided     = false,
        });
    }
}
