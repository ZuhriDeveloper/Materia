using Materia.Domain.Sales;

namespace Materia.Infrastructure.Persistence.Projections;

/// <summary>
/// Read model / payment history for receivable collections (pelunasan piutang).
/// Doubles as the idempotency store: <see cref="IdempotencyKey"/> has a unique index so a
/// retried or concurrent duplicate submission can never persist a second payment.
/// Rebuildable from the <c>ReceivablePaymentRecorded</c> event stream.
/// </summary>
public class ReceivablePaymentReadModel
{
    /// <summary>Server-issued payment id (the event's PaymentId).</summary>
    public Guid          Id              { get; set; }

    /// <summary>Client-supplied de-duplication token. Unique.</summary>
    public Guid          IdempotencyKey  { get; set; }

    public Guid          CustomerId      { get; set; }
    public decimal       Amount          { get; set; }
    public decimal       NewBalance      { get; set; }
    public PaymentMethod Method          { get; set; }
    public string?       Notes           { get; set; }
    public string        ReceivedBy      { get; set; } = default!;
    public DateTime      RecordedAt      { get; set; }

    /// <summary>FIFO allocations baked into the event, stored as JSON for response replay.</summary>
    public string        AllocationsJson { get; set; } = default!;
}
