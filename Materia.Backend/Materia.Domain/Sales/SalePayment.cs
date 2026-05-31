using Materia.Domain.Common;

namespace Materia.Domain.Sales;

public record SalePayment
{
    public Money         PaidAmount { get; }
    public Money         Change     { get; }
    public PaymentMethod Method     { get; }
    public DateTime      PaidAt     { get; }

    public SalePayment(Money paidAmount, Money grandTotal, PaymentMethod method, DateTime paidAt)
    {
        if (paidAmount.Amount < grandTotal.Amount)
            throw new DomainException(
                $"Pembayaran kurang. Total: {grandTotal}, Dibayar: {paidAmount}.");

        PaidAmount = paidAmount;
        Change     = new Money(paidAmount.Amount - grandTotal.Amount);
        Method     = method;
        PaidAt     = paidAt;
    }

    // Reconstitution constructor (from stored event data)
    internal SalePayment(decimal paidAmount, decimal change, PaymentMethod method, DateTime paidAt)
    {
        PaidAmount = new Money(paidAmount);
        Change     = new Money(change);
        Method     = method;
        PaidAt     = paidAt;
    }
}
