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

            // Line-pricing Phase 1 rules
            item.RuleFor(i => i.DiscountPerUnit)
                .GreaterThanOrEqualTo(0)
                .When(i => i.DiscountPerUnit.HasValue)
                .WithMessage("Diskon per unit tidak boleh negatif.");

            // When ListUnitPrice is provided it must be >= UnitPrice (net = list - discount)
            item.RuleFor(i => i.ListUnitPrice)
                .GreaterThanOrEqualTo(i => i.UnitPrice)
                .When(i => i.ListUnitPrice.HasValue)
                .WithMessage("Harga list tidak boleh lebih kecil dari harga jual bersih.");

            // Discount must not exceed the list price
            item.RuleFor(i => i.DiscountPerUnit)
                .LessThanOrEqualTo(i => i.ListUnitPrice ?? i.UnitPrice)
                .When(i => i.DiscountPerUnit.HasValue)
                .WithMessage("Diskon tidak boleh melebihi harga list.");

            // H1 — net-price consistency: when ListUnitPrice and/or DiscountPerUnit is
            // supplied, UnitPrice must equal the derived net (ListUnitPrice - DiscountPerUnit)
            // within Money's 2-dp rounding tolerance (≤ 0.01).
            item.RuleFor(i => i.UnitPrice)
                .Must((i, unitPrice) =>
                {
                    // Only check when at least one of the optional fields is present
                    if (!i.ListUnitPrice.HasValue && !i.DiscountPerUnit.HasValue)
                        return true;

                    var list    = i.ListUnitPrice ?? unitPrice;
                    var disc    = i.DiscountPerUnit ?? 0m;
                    var netPrice = list - disc;
                    return Math.Abs(unitPrice - netPrice) <= 0.01m;
                })
                .When(i => i.ListUnitPrice.HasValue || i.DiscountPerUnit.HasValue)
                .WithMessage("Harga jual tidak konsisten dengan harga sebelum diskon dikurangi diskon.");
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

        // Use the same net price the domain will charge:
        // (ListUnitPrice ?? UnitPrice) - (DiscountPerUnit ?? 0)
        // After the H1 consistency rule this equals UnitPrice, but compute
        // defensively so credit classification is never based on the sticker price.
        var subtotal = c.Items.Sum(i =>
        {
            var list = i.ListUnitPrice ?? i.UnitPrice;
            var disc = i.DiscountPerUnit ?? 0m;
            return (list - disc) * i.Quantity;
        });
        var discount = Math.Clamp(c.Discount, 0m, subtotal);
        var taxable  = subtotal - discount;
        var tax      = c.TaxEnabled ? Math.Round(taxable * Domain.Sales.Sale.TaxRate, 0, MidpointRounding.AwayFromZero) : 0m;
        var grandTotal = taxable + tax;

        var isCredit = paid < grandTotal;
        return !isCredit || c.CustomerId is not null;
    }
}
