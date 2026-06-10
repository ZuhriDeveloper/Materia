using Materia.Domain.Common;
using Materia.Domain.Financials.Events;

namespace Materia.Domain.Financials;

/// <summary>
/// Records cash taken out of the change fund (uang kembalian) — the compensating
/// counterpart of <see cref="ChangeFundDeposit"/>, used by an Admin to correct an
/// erroneous deposit without touching the append-only event history.
/// Immutable once recorded — record-only aggregate like ChangeFundDeposit.
/// </summary>
public sealed class ChangeFundWithdrawal : AggregateRoot<ChangeFundWithdrawalId>
{
    public decimal  Amount     { get; private set; }

    /// <summary>Why money left the fund — mandatory, this is the audit trail for corrections.</summary>
    public string   Reason     { get; private set; } = default!;
    public string   RecordedBy { get; private set; } = default!;
    public DateTime RecordedAt { get; private set; }

    /// <summary>Client-supplied de-duplication token (see <see cref="ChangeFundWithdrawn.IdempotencyKey"/>).</summary>
    public Guid     IdempotencyKey { get; private set; }

    private ChangeFundWithdrawal() { }

    // ── Factories ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The aggregate holds no balance knowledge — "withdrawal must not exceed the current
    /// balance" is enforced by the application handler against the read model.
    /// <paramref name="idempotencyKey"/> is the client's double-submit guard; when omitted,
    /// a fresh key is generated so internal callers still produce a non-empty value.
    /// </summary>
    public static ChangeFundWithdrawal Record(
        decimal amount,
        string  reason,
        string  recordedBy,
        Guid?   idempotencyKey = null)
    {
        if (amount <= 0)
            throw new DomainException("Jumlah harus lebih dari nol.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Alasan penarikan wajib diisi.");

        if (string.IsNullOrWhiteSpace(recordedBy))
            throw new DomainException("Pencatat wajib diisi.");

        var withdrawal = new ChangeFundWithdrawal();
        withdrawal.Raise(new ChangeFundWithdrawn(
            ChangeFundWithdrawalId.New(),
            Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            reason.Trim(),
            recordedBy.Trim(),
            DateTime.UtcNow,
            idempotencyKey is { } k && k != Guid.Empty ? k : Guid.NewGuid()));
        return withdrawal;
    }

    public static ChangeFundWithdrawal Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var withdrawal = new ChangeFundWithdrawal();
        withdrawal.Load(events);
        return withdrawal;
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case ChangeFundWithdrawn e:
                Id             = e.Id;
                Amount         = e.Amount;
                Reason         = e.Reason;
                RecordedBy     = e.RecordedBy;
                RecordedAt     = e.OccurredAt;
                IdempotencyKey = e.IdempotencyKey;
                break;
        }
    }
}
