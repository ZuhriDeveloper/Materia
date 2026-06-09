using Materia.Domain.Common;

namespace Materia.Domain.Sales;

public record SalePayment
{
    public Money         PaidAmount  { get; }
    public Money         Change      { get; }
    public Money         Outstanding { get; }
    public PaymentMethod Method      { get; }
    public DateTime      PaidAt      { get; }

    /// <summary>
    /// Full payment (legacy checkout): requires <paramref name="paidAmount"/> ≥
    /// <paramref name="grandTotal"/>, yields change, and leaves no outstanding balance.
    /// </summary>
    public SalePayment(Money paidAmount, Money grandTotal, PaymentMethod method, DateTime paidAt)
        : this(paidAmount, grandTotal, method, paidAt, allowPartial: false) { }

    private SalePayment(
        Money paidAmount, Money grandTotal, PaymentMethod method, DateTime paidAt, bool allowPartial)
    {
        if (!allowPartial && paidAmount.Amount < grandTotal.Amount)
            throw new DomainException(
                $"Pembayaran kurang. Total: {grandTotal}, Dibayar: {paidAmount}.");

        PaidAmount  = paidAmount;
        Change      = new Money(Math.Max(0m, paidAmount.Amount - grandTotal.Amount));
        Outstanding = new Money(Math.Max(0m, grandTotal.Amount - paidAmount.Amount));
        Method      = method;
        PaidAt      = paidAt;
    }

    /// <summary>
    /// Settlement that permits a partial (credit / Bon) payment — the resulting
    /// <see cref="Outstanding"/> may be greater than zero.
    /// </summary>
    public static SalePayment Settle(
        Money paidAmount, Money grandTotal, PaymentMethod method, DateTime paidAt)
        => new(paidAmount, grandTotal, method, paidAt, allowPartial: true);

    // Reconstitution constructor (from stored event data)
    internal SalePayment(
        decimal paidAmount, decimal change, decimal outstanding, PaymentMethod method, DateTime paidAt)
    {
        PaidAmount  = new Money(paidAmount);
        Change      = new Money(change);
        Outstanding = new Money(outstanding);
        Method      = method;
        PaidAt      = paidAt;
    }
}
