using FluentValidation;

namespace Materia.Application.Commands.Sales.FinalizeSale;

public sealed class FinalizeSaleCommandValidator : AbstractValidator<FinalizeSaleCommand>
{
    public FinalizeSaleCommandValidator()
    {
        RuleFor(x => x.ServedBy).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerName).MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Penjualan harus memiliki minimal satu item.");

        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0)
            .WithMessage("Diskon tidak boleh negatif.");
        RuleFor(x => x.AmountPaid).GreaterThanOrEqualTo(0)
            .When(x => x.AmountPaid.HasValue)
            .WithMessage("Jumlah pembayaran tidak boleh negatif.");
        // Sanity cap — a tender beyond Rp 1 miliar is a typo, not a payment.
        RuleFor(x => x.AmountPaid).LessThanOrEqualTo(1_000_000_000m)
            .When(x => x.AmountPaid.HasValue)
            .WithMessage("Jumlah pembayaran melebihi batas wajar (maks. Rp 1.000.000.000).");

        // A credit sale (outstanding balance) is only allowed for a registered customer.
        // The domain enforces this too; validating here returns a clean 400 to the client.
        RuleFor(x => x)
            .Must(HasCustomerWhenCredit)
            .WithMessage("Penjualan dengan piutang (bon) hanya tersedia untuk pelanggan terdaftar.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.UnitName).NotEmpty().MaximumLength(50);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }

    /// <summary>
    /// A null AmountPaid means full payment (no credit). When an explicit amount is given that
    /// is below the grand total, the remainder is customer debt — which requires a customer.
    /// </summary>
    private static bool HasCustomerWhenCredit(FinalizeSaleCommand c)
    {
        if (c.AmountPaid is not { } paid) return true;        // full payment
        if (c.Items is null || c.Items.Count == 0) return true; // item rule reports this

        var subtotal = c.Items.Sum(i => i.UnitPrice * i.Quantity);
        var discount = Math.Clamp(c.Discount, 0m, subtotal);
        var taxable  = subtotal - discount;
        var tax      = c.TaxEnabled ? Math.Round(taxable * Domain.Sales.Sale.TaxRate, 0, MidpointRounding.AwayFromZero) : 0m;
        var grandTotal = taxable + tax;

        var isCredit = paid < grandTotal;
        return !isCredit || c.CustomerId is not null;
    }
}
