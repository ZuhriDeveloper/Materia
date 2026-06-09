using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Customers;
using Materia.Domain.Customers.Events;
using Materia.Domain.Sales;

namespace Materia.Tests.Customers;

public class ReceivablePaymentTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static Customer ActiveCustomerWithDebt(params (decimal amount, string refNo)[] invoices)
    {
        var customer = Customer.Create("Budi Santoso", "08123456789", null, "admin");
        foreach (var (amount, refNo) in invoices)
            customer.IncurDebt(amount, Guid.NewGuid(), refNo, "kasir-01");
        customer.ClearDomainEvents();
        return customer;
    }

    // ── RecordRepayment: basic happy path ─────────────────────────────────────

    [Fact]
    public void RecordRepayment_ReducesOutstandingDebtAndRaisesEvent()
    {
        var customer = ActiveCustomerWithDebt((100_000m, "INV-1"));

        var paymentId = customer.RecordRepayment(60_000m, PaymentMethod.Cash, null, "kasir-01");

        customer.OutstandingDebt.Should().Be(40_000m);

        var evt = customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ReceivablePaymentRecorded>().Subject;

        evt.PaymentId.Should().Be(paymentId);
        evt.Amount.Should().Be(60_000m);
        evt.NewBalance.Should().Be(40_000m);
        evt.ReceivedBy.Should().Be("kasir-01");
        evt.Method.Should().Be(PaymentMethod.Cash);
    }

    [Fact]
    public void RecordRepayment_SingleInvoiceFullPayoff_ClearsLine()
    {
        var customer = ActiveCustomerWithDebt((100_000m, "INV-1"));

        customer.RecordRepayment(100_000m, PaymentMethod.BankTransfer, "lunas", "kasir-01");

        customer.OutstandingDebt.Should().Be(0m);
        customer.OpenReceivables.Should().BeEmpty();

        var evt = customer.DomainEvents.OfType<ReceivablePaymentRecorded>().Single();
        evt.Allocations.Should().HaveCount(1);
        evt.Allocations[0].AppliedAmount.Should().Be(100_000m);
        evt.Allocations[0].RemainingAfter.Should().Be(0m);
        evt.Notes.Should().Be("lunas");
    }

    // ── FIFO allocation ───────────────────────────────────────────────────────

    [Fact]
    public void RecordRepayment_FifoAllocation_OldestInvoiceSettledFirst()
    {
        // INV-1 = 100 000 (oldest), INV-2 = 50 000
        var customer = ActiveCustomerWithDebt(
            (100_000m, "INV-1"),
            (50_000m,  "INV-2"));

        customer.RecordRepayment(120_000m, PaymentMethod.Cash, null, "kasir-01");

        customer.OutstandingDebt.Should().Be(30_000m);

        var evt = customer.DomainEvents.OfType<ReceivablePaymentRecorded>().Single();
        evt.Allocations.Should().HaveCount(2);

        var inv1 = evt.Allocations.First(a => a.ReferenceNo == "INV-1");
        inv1.AppliedAmount.Should().Be(100_000m);
        inv1.RemainingAfter.Should().Be(0m);

        var inv2 = evt.Allocations.First(a => a.ReferenceNo == "INV-2");
        inv2.AppliedAmount.Should().Be(20_000m);
        inv2.RemainingAfter.Should().Be(30_000m);

        // OpenReceivables should still contain INV-2 with 30 000 remaining
        customer.OpenReceivables.Should().HaveCount(1);
        customer.OpenReceivables[0].ReferenceNo.Should().Be("INV-2");
        customer.OpenReceivables[0].RemainingAmount.Should().Be(30_000m);
    }

    [Fact]
    public void RecordRepayment_FifoAllocation_ExactThreeInvoices()
    {
        var customer = ActiveCustomerWithDebt(
            (50_000m,  "INV-A"),
            (50_000m,  "INV-B"),
            (50_000m,  "INV-C"));

        // Pay exactly enough to clear INV-A and INV-B
        customer.RecordRepayment(100_000m, PaymentMethod.QRIS, null, "kasir-01");

        customer.OutstandingDebt.Should().Be(50_000m);

        var evt = customer.DomainEvents.OfType<ReceivablePaymentRecorded>().Single();
        evt.Allocations.Should().HaveCount(2); // only INV-A and INV-B touched

        customer.OpenReceivables.Should().HaveCount(1);
        customer.OpenReceivables[0].ReferenceNo.Should().Be("INV-C");
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void RecordRepayment_ZeroOrNegativeAmount_ThrowsDomainException(decimal amount)
    {
        var customer = ActiveCustomerWithDebt((100_000m, "INV-1"));

        Action act = () => customer.RecordRepayment(amount, PaymentMethod.Cash, null, "kasir-01");

        act.Should().Throw<DomainException>()
           .WithMessage("*lebih dari nol*");
    }

    [Fact]
    public void RecordRepayment_WithNoOutstandingDebt_ThrowsDomainException()
    {
        var customer = Customer.Create("Budi", "08123456789", null, "admin");

        Action act = () => customer.RecordRepayment(10_000m, PaymentMethod.Cash, null, "kasir-01");

        act.Should().Throw<DomainException>()
           .WithMessage("*sisa piutang*");
    }

    [Fact]
    public void RecordRepayment_AmountExceedsOutstandingDebt_ThrowsDomainException()
    {
        var customer = ActiveCustomerWithDebt((50_000m, "INV-1"));

        Action act = () => customer.RecordRepayment(100_000m, PaymentMethod.Cash, null, "kasir-01");

        act.Should().Throw<DomainException>()
           .WithMessage("*melebihi sisa piutang*");
    }

    // ── Inactive customer ─────────────────────────────────────────────────────

    [Fact]
    public void RecordRepayment_WhenCustomerIsInactive_Succeeds()
    {
        // Repayment must succeed on inactive customers — the debt is real and was already
        // incurred when the customer was active. Blocking would strand the outstanding balance.
        var customer = ActiveCustomerWithDebt((80_000m, "INV-1"));
        customer.Deactivate("admin");
        customer.ClearDomainEvents();

        Action act = () => customer.RecordRepayment(80_000m, PaymentMethod.Cash, null, "kasir-01");

        act.Should().NotThrow();
        customer.OutstandingDebt.Should().Be(0m);
    }

    // ── Notes trimming ────────────────────────────────────────────────────────

    [Fact]
    public void RecordRepayment_NotesAreTrimmed()
    {
        var customer = ActiveCustomerWithDebt((50_000m, "INV-1"));

        customer.RecordRepayment(50_000m, PaymentMethod.Cash, "  DP pelunasan  ", "kasir-01");

        var evt = customer.DomainEvents.OfType<ReceivablePaymentRecorded>().Single();
        evt.Notes.Should().Be("DP pelunasan");
    }

    [Fact]
    public void RecordRepayment_BlankNotesStoredAsNull()
    {
        var customer = ActiveCustomerWithDebt((50_000m, "INV-1"));

        customer.RecordRepayment(50_000m, PaymentMethod.Cash, "   ", "kasir-01");

        var evt = customer.DomainEvents.OfType<ReceivablePaymentRecorded>().Single();
        evt.Notes.Should().BeNull();
    }

    // ── Idempotency key ───────────────────────────────────────────────────────

    [Fact]
    public void RecordRepayment_WithSuppliedIdempotencyKey_BakesKeyIntoEvent()
    {
        var customer = ActiveCustomerWithDebt((100_000m, "INV-1"));
        var key = Guid.NewGuid();

        customer.RecordRepayment(60_000m, PaymentMethod.Cash, null, "kasir-01", key);

        var evt = customer.DomainEvents.OfType<ReceivablePaymentRecorded>().Single();
        evt.IdempotencyKey.Should().Be(key);
        // PaymentId is a distinct, server-issued surrogate — never the client's key.
        evt.PaymentId.Should().NotBe(key);
    }

    [Fact]
    public void RecordRepayment_WithoutIdempotencyKey_GeneratesNonEmptyKey()
    {
        // Internal/test callers that don't pass a key still produce a unique, non-empty
        // key so the payment projection's unique index never sees Guid.Empty collisions.
        var customer = ActiveCustomerWithDebt((100_000m, "INV-1"));

        customer.RecordRepayment(60_000m, PaymentMethod.Cash, null, "kasir-01");

        var evt = customer.DomainEvents.OfType<ReceivablePaymentRecorded>().Single();
        evt.IdempotencyKey.Should().NotBeEmpty();
    }

    // ── Reconstitute ──────────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_ReplaysRepayments_RestoresOutstandingDebtAndLines()
    {
        var original = Customer.Create("Budi", "08123456789", null, "admin");
        original.IncurDebt(100_000m, Guid.NewGuid(), "INV-1", "kasir-01");
        original.IncurDebt(50_000m,  Guid.NewGuid(), "INV-2", "kasir-01");
        original.RecordRepayment(120_000m, PaymentMethod.Cash, null, "kasir-01");

        // OutstandingDebt = 150 000 - 120 000 = 30 000
        // INV-1 fully cleared; INV-2 has 30 000 remaining

        var replayed = Customer.Reconstitute(original.DomainEvents);

        replayed.OutstandingDebt.Should().Be(30_000m);
        replayed.OpenReceivables.Should().HaveCount(1);
        replayed.OpenReceivables[0].RemainingAmount.Should().Be(30_000m);
        replayed.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reconstitute_MultipleRepaymentsInSequence_FullyDrainDebt()
    {
        var original = Customer.Create("Budi", "08123456789", null, "admin");
        original.IncurDebt(100_000m, Guid.NewGuid(), "INV-1", "kasir-01");
        original.RecordRepayment(60_000m, PaymentMethod.Cash, null, "kasir-01");
        original.RecordRepayment(40_000m, PaymentMethod.Cash, null, "kasir-01");

        var replayed = Customer.Reconstitute(original.DomainEvents);

        replayed.OutstandingDebt.Should().Be(0m);
        replayed.OpenReceivables.Should().BeEmpty();
    }
}
