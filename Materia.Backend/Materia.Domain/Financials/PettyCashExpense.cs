using Materia.Domain.Common;
using Materia.Domain.Financials.Events;

namespace Materia.Domain.Financials;

/// <summary>
/// A petty cash (kas kecil) expense — cash paid out of the drawer for something
/// outside normal purchasing. Immutable once recorded in this increment.
/// </summary>
public sealed class PettyCashExpense : AggregateRoot<PettyCashExpenseId>
{
    public decimal           Amount       { get; private set; }
    public string            Recipient    { get; private set; } = default!;
    public PettyCashCategory Category      { get; private set; }
    public string?           ReasonDetail { get; private set; }
    public string?           Notes        { get; private set; }
    public string            ReferenceNo  { get; private set; } = default!;
    public string            RecordedBy   { get; private set; } = default!;
    public DateTime          RecordedAt   { get; private set; }

    /// <summary>Client-supplied de-duplication token (see <see cref="PettyCashExpenseRecorded.IdempotencyKey"/>).</summary>
    public Guid              IdempotencyKey { get; private set; }

    /// <summary>Resolved display reason: the free-text detail for Lainnya, else the category label.</summary>
    public string Reason =>
        Category == PettyCashCategory.Lainnya
            ? (ReasonDetail ?? PettyCashCategory.Lainnya.ToDisplayLabel())
            : Category.ToDisplayLabel();

    private PettyCashExpense() { }

    // ── Factories ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <paramref name="idempotencyKey"/> is the client's double-submit guard, baked into the
    /// event so the projection's unique index can reject duplicates. The domain does not
    /// enforce idempotency itself; when omitted, a fresh key is generated so internal
    /// callers still produce a unique, non-empty value.
    /// </summary>
    public static PettyCashExpense Record(
        decimal           amount,
        string            recipient,
        PettyCashCategory category,
        string?           reasonDetail,
        string?           notes,
        string            referenceNo,
        string            recordedBy,
        Guid?             idempotencyKey = null)
    {
        if (amount <= 0)
            throw new DomainException("Jumlah pengeluaran harus lebih dari nol.");

        if (string.IsNullOrWhiteSpace(recipient))
            throw new DomainException("Penerima wajib diisi.");

        if (string.IsNullOrWhiteSpace(referenceNo))
            throw new DomainException("Nomor referensi wajib diisi.");

        if (string.IsNullOrWhiteSpace(recordedBy))
            throw new DomainException("Pencatat wajib diisi.");

        // "Sebutkan" is mandatory only for the free-text "Lainnya" template.
        // For predefined templates the category itself is the reason, so any
        // stray detail is discarded to keep the record unambiguous.
        var detail = category == PettyCashCategory.Lainnya
            ? reasonDetail?.Trim()
            : null;

        if (category == PettyCashCategory.Lainnya && string.IsNullOrWhiteSpace(detail))
            throw new DomainException("Sebutkan alasan untuk kategori 'Lainnya'.");

        var expense = new PettyCashExpense();
        expense.Raise(new PettyCashExpenseRecorded(
            PettyCashExpenseId.New(),
            Math.Round(amount, 2),
            recipient.Trim(),
            category,
            detail,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            referenceNo.Trim(),
            recordedBy,
            DateTime.UtcNow,
            idempotencyKey is { } k && k != Guid.Empty ? k : Guid.NewGuid()));
        return expense;
    }

    public static PettyCashExpense Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var expense = new PettyCashExpense();
        expense.Load(events);
        return expense;
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case PettyCashExpenseRecorded e:
                Id             = e.Id;
                Amount         = e.Amount;
                Recipient      = e.Recipient;
                Category       = e.Category;
                ReasonDetail   = e.ReasonDetail;
                Notes          = e.Notes;
                ReferenceNo    = e.ReferenceNo;
                RecordedBy     = e.RecordedBy;
                RecordedAt     = e.OccurredAt;
                IdempotencyKey = e.IdempotencyKey;
                break;
        }
    }
}
