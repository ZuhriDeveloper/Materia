using Materia.Application.Contracts.Financials;
using Materia.Domain.Financials;

namespace Materia.Tests.Financials;

/// <summary>
/// In-memory <see cref="IChangeFundRepository"/> mimicking the real repository's
/// idempotency contract: SaveAsync registers the entry under its key, find-by-key
/// returns it on a resend, and an armed "race" simulates a concurrent duplicate
/// committing first (surfaced as <see cref="DuplicateChangeFundEntryException"/>).
/// </summary>
internal sealed class FakeChangeFundRepository : IChangeFundRepository
{
    private readonly Dictionary<Guid, Guid> _depositsByKey    = new();
    private readonly Dictionary<Guid, Guid> _withdrawalsByKey = new();

    private Guid _raceKey;
    private Guid _racePriorId;
    private bool _raceArmed;
    private bool _raceCommitted;

    public int                   DepositSaveCount    { get; private set; }
    public int                   WithdrawalSaveCount { get; private set; }
    public ChangeFundDeposit?    LastDeposit         { get; private set; }
    public ChangeFundWithdrawal? LastWithdrawal      { get; private set; }

    public void ArmRace(Guid key, Guid priorId)
    {
        _raceKey     = key;
        _racePriorId = priorId;
        _raceArmed   = true;
    }

    public Task SaveAsync(ChangeFundDeposit deposit, CancellationToken ct = default)
    {
        ThrowIfRaceArmed();
        DepositSaveCount++;
        LastDeposit = deposit;
        _depositsByKey[deposit.IdempotencyKey] = deposit.Id.Value;
        deposit.ClearDomainEvents();
        return Task.CompletedTask;
    }

    public Task SaveAsync(ChangeFundWithdrawal withdrawal, CancellationToken ct = default)
    {
        ThrowIfRaceArmed();
        WithdrawalSaveCount++;
        LastWithdrawal = withdrawal;
        _withdrawalsByKey[withdrawal.IdempotencyKey] = withdrawal.Id.Value;
        withdrawal.ClearDomainEvents();
        return Task.CompletedTask;
    }

    public Task<Guid?> FindDepositIdByKeyAsync(Guid idempotencyKey, CancellationToken ct = default)
        => FindIn(_depositsByKey, idempotencyKey);

    public Task<Guid?> FindWithdrawalIdByKeyAsync(Guid idempotencyKey, CancellationToken ct = default)
        => FindIn(_withdrawalsByKey, idempotencyKey);

    private void ThrowIfRaceArmed()
    {
        if (!_raceArmed) return;
        // Simulate the unique-index violation surfaced by a concurrent winner.
        _raceArmed     = false;
        _raceCommitted = true;
        throw new DuplicateChangeFundEntryException(_raceKey);
    }

    private Task<Guid?> FindIn(Dictionary<Guid, Guid> store, Guid key)
    {
        if (store.TryGetValue(key, out var id))
            return Task.FromResult<Guid?>(id);

        // The racing transaction only becomes visible once it has "committed".
        if (_raceCommitted && key == _raceKey)
            return Task.FromResult<Guid?>(_racePriorId);

        return Task.FromResult<Guid?>(null);
    }
}
