using Materia.Domain.Common;

namespace Materia.Domain.Customers;

/// <summary>
/// A single open (or partially-paid) receivable line on a customer account: either a credit
/// sale or an opening receivable (saldo awal) recorded without a sale.
/// Insertion order is chronological (oldest first), which drives the FIFO allocation.
/// </summary>
public sealed class ReceivableLine
{
    /// <summary>
    /// The credit sale's id, or the opening receivable's id for lines without a sale.
    /// Name kept for compatibility with stored allocation events and payment history.
    /// </summary>
    public Guid     SaleId          { get; }
    public string   ReferenceNo     { get; }
    public decimal  OriginalAmount  { get; }
    public decimal  RemainingAmount { get; private set; }
    public DateTime IncurredAt      { get; }

    public ReceivableLine(
        Guid saleId, string referenceNo,
        decimal originalAmount, decimal remainingAmount,
        DateTime incurredAt)
    {
        SaleId          = saleId;
        ReferenceNo     = referenceNo;
        OriginalAmount  = originalAmount;
        RemainingAmount = remainingAmount;
        IncurredAt      = incurredAt;
    }

    /// <summary>Applies a partial or full payment against this line.</summary>
    internal void ApplyPayment(decimal applied)
    {
        // Guards against a corrupt/forged allocation driving the remainder negative on replay.
        // Valid FIFO allocations are always within [0, RemainingAmount], so this never trips
        // on legitimate event streams.
        if (applied < 0 || applied > RemainingAmount)
            throw new DomainException("Alokasi pembayaran tidak valid untuk baris piutang ini.");

        RemainingAmount -= applied;
    }
}
