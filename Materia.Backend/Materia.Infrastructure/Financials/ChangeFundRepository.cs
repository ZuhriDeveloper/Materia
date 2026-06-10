using Materia.Application.Contracts.Financials;
using Materia.Application.Contracts.Stores;
using Materia.Domain.Financials;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Materia.Infrastructure.Financials;

public class ChangeFundRepository(AppDbContext context, ICurrentStore currentStore)
    : IChangeFundRepository
{
    private const string DepositAggregateType    = "ChangeFundDeposit";
    private const string WithdrawalAggregateType = "ChangeFundWithdrawal";

    public async Task SaveAsync(ChangeFundDeposit deposit, CancellationToken ct = default)
    {
        var newEvents = deposit.DomainEvents;
        if (newEvents.Count == 0) return;

        var storeId = currentStore.StoreId;
        AppendEvents(DepositAggregateType, deposit.Id.Value,
            deposit.Version - newEvents.Count, newEvents, storeId);

        context.ChangeFundDepositReadModels.Add(new ChangeFundDepositReadModel
        {
            Id                = deposit.Id.Value,
            StoreId           = storeId,
            Amount            = deposit.Amount,
            Source            = deposit.Source,
            SourceReferenceNo = deposit.SourceReferenceNo,
            Notes             = deposit.Notes,
            RecordedBy        = deposit.RecordedBy,
            RecordedAt        = deposit.RecordedAt,
            IdempotencyKey    = deposit.IdempotencyKey,
        });

        await SaveTranslatingDuplicateKeyAsync(deposit.IdempotencyKey, ct);
        deposit.ClearDomainEvents();
    }

    public async Task SaveAsync(ChangeFundWithdrawal withdrawal, CancellationToken ct = default)
    {
        var newEvents = withdrawal.DomainEvents;
        if (newEvents.Count == 0) return;

        var storeId = currentStore.StoreId;
        AppendEvents(WithdrawalAggregateType, withdrawal.Id.Value,
            withdrawal.Version - newEvents.Count, newEvents, storeId);

        context.ChangeFundWithdrawalReadModels.Add(new ChangeFundWithdrawalReadModel
        {
            Id             = withdrawal.Id.Value,
            StoreId        = storeId,
            Amount         = withdrawal.Amount,
            Reason         = withdrawal.Reason,
            RecordedBy     = withdrawal.RecordedBy,
            RecordedAt     = withdrawal.RecordedAt,
            IdempotencyKey = withdrawal.IdempotencyKey,
        });

        await SaveTranslatingDuplicateKeyAsync(withdrawal.IdempotencyKey, ct);
        withdrawal.ClearDomainEvents();
    }

    public async Task<Guid?> FindDepositIdByKeyAsync(
        Guid idempotencyKey, CancellationToken ct = default)
    {
        var row = await context.ChangeFundDepositReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.IdempotencyKey == idempotencyKey, ct);
        return row?.Id;
    }

    public async Task<Guid?> FindWithdrawalIdByKeyAsync(
        Guid idempotencyKey, CancellationToken ct = default)
    {
        var row = await context.ChangeFundWithdrawalReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.IdempotencyKey == idempotencyKey, ct);
        return row?.Id;
    }

    private void AppendEvents(
        string aggregateType, Guid aggregateId, long baseVersion,
        IReadOnlyList<Domain.Common.IDomainEvent> newEvents, Guid storeId)
    {
        for (var i = 0; i < newEvents.Count; i++)
        {
            var evt = newEvents[i];
            context.StoredEvents.Add(new StoredEvent
            {
                StoreId       = storeId,
                AggregateType = aggregateType,
                AggregateId   = aggregateId,
                Version       = baseVersion + i + 1,
                EventType     = EventTypeRegistry.GetName(evt),
                EventData     = EventSerializer.Serialize(evt),
                OccurredAt    = evt.OccurredAt,
            });
        }
    }

    /// <summary>
    /// Translate a duplicate-idempotency-key collision (a concurrent entry with the same
    /// client key committed first) into an application-level signal the handler can replay,
    /// without leaking the persistence technology into the Application layer.
    /// </summary>
    private async Task SaveTranslatingDuplicateKeyAsync(Guid idempotencyKey, CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsIdempotencyKeyViolation(ex))
        {
            throw new DuplicateChangeFundEntryException(idempotencyKey);
        }
    }

    private static bool IsIdempotencyKeyViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg &&
        (pg.ConstraintName?.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase) ?? false);
}
