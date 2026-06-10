using Materia.Domain.Common;
using Materia.Domain.Financials.Events;

namespace Materia.Domain.Financials;

/// <summary>
/// Records cash deposited into the change fund (uang kembalian) so the system
/// tracks the available float for giving change to customers.
/// Immutable once recorded — record-only aggregate like PettyCashExpense.
/// </summary>
public sealed class ChangeFundDeposit : AggregateRoot<ChangeFundDepositId>
{
    public decimal          Amount            { get; private set; }
    public ChangeFundSource Source            { get; private set; }

    /// <summary>Petty cash ReferenceNo when Source == PettyCashExchange; null for ManualEntry.</summary>
    public string?          SourceReferenceNo { get; private set; }
    public string?          Notes             { get; private set; }
    public string           RecordedBy        { get; private set; } = default!;
    public DateTime         RecordedAt        { get; private set; }

    /// <summary>Client-supplied de-duplication token (see <see cref="ChangeFundDeposited.IdempotencyKey"/>).</summary>
    public Guid             IdempotencyKey    { get; private set; }

    private ChangeFundDeposit() { }

    // ── Factories ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <paramref name="idempotencyKey"/> is the client's double-submit guard, baked into the
    /// event so the projection's unique index can reject duplicates. The domain does not
    /// enforce idempotency itself (it holds no key history); when omitted, a fresh key is
    /// generated so internal callers still produce a unique, non-empty value.
    /// </summary>
    public static ChangeFundDeposit Record(
        decimal          amount,
        ChangeFundSource source,
        string?          sourceReferenceNo,
        string?          notes,
        string           recordedBy,
        Guid?            idempotencyKey = null)
    {
        if (amount <= 0)
            throw new DomainException("Jumlah harus lebih dari nol.");

        if (string.IsNullOrWhiteSpace(recordedBy))
            throw new DomainException("Pencatat wajib diisi.");

        if (source == ChangeFundSource.PettyCashExchange
            && string.IsNullOrWhiteSpace(sourceReferenceNo))
            throw new DomainException("Nomor referensi wajib diisi untuk sumber kas kecil.");

        var deposit = new ChangeFundDeposit();
        deposit.Raise(new ChangeFundDeposited(
            ChangeFundDepositId.New(),
            Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            source,
            source == ChangeFundSource.PettyCashExchange ? sourceReferenceNo!.Trim() : null,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            recordedBy.Trim(),
            DateTime.UtcNow,
            idempotencyKey is { } k && k != Guid.Empty ? k : Guid.NewGuid()));
        return deposit;
    }

    public static ChangeFundDeposit Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var deposit = new ChangeFundDeposit();
        deposit.Load(events);
        return deposit;
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case ChangeFundDeposited e:
                Id                = e.Id;
                Amount            = e.Amount;
                Source            = e.Source;
                SourceReferenceNo = e.SourceReferenceNo;
                Notes             = e.Notes;
                RecordedBy        = e.RecordedBy;
                RecordedAt        = e.OccurredAt;
                IdempotencyKey    = e.IdempotencyKey;
                break;
        }
    }
}
