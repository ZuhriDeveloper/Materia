using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Customers;
using Materia.Domain.Customers.Events;
using Materia.Domain.Sales;

namespace Materia.Tests.Customers;

public class OpeningReceivableTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static readonly DateTime OldDate = new(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc);

    private static Customer ActiveCustomer()
    {
        var customer = Customer.Create("Budi Santoso", "08123456789", null, "admin");
        customer.ClearDomainEvents();
        return customer;
    }

    // ── RecordOpeningReceivable: happy path ───────────────────────────────────

    [Fact]
    public void RecordOpeningReceivable_IncreasesDebtAndRaisesEvent()
    {
        var customer     = ActiveCustomer();
        var receivableId = Guid.NewGuid();

        customer.RecordOpeningReceivable(
            receivableId, 250_000m, OldDate, " NOTA-LAMA-01 ", " bon buku lama ", "admin");

        customer.OutstandingDebt.Should().Be(250_000m);

        var evt = customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OpeningReceivableRecorded>().Subject;

        evt.CustomerId.Should().Be(customer.Id);
        evt.ReceivableId.Should().Be(receivableId);
        evt.ReferenceNo.Should().Be("NOTA-LAMA-01");
        evt.Amount.Should().Be(250_000m);
        evt.IncurredAt.Should().Be(OldDate);
        evt.NewBalance.Should().Be(250_000m);
        evt.Notes.Should().Be("bon buku lama");
        evt.RecordedBy.Should().Be("admin");
    }

    [Fact]
    public void RecordOpeningReceivable_AddsOpenLineDatedAtOriginalDate()
    {
        var customer     = ActiveCustomer();
        var receivableId = Guid.NewGuid();

        customer.RecordOpeningReceivable(receivableId, 250_000m, OldDate, "NOTA-LAMA-01", null, "admin");

        var line = customer.OpenReceivables.Should().ContainSingle().Subject;
        line.SaleId.Should().Be(receivableId);
        line.ReferenceNo.Should().Be("NOTA-LAMA-01");
        line.OriginalAmount.Should().Be(250_000m);
        line.RemainingAmount.Should().Be(250_000m);
        line.IncurredAt.Should().Be(OldDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordOpeningReceivable_BlankReference_DefaultsToSaldoAwal(string? referenceNo)
    {
        var customer = ActiveCustomer();

        customer.RecordOpeningReceivable(Guid.NewGuid(), 100_000m, OldDate, referenceNo, "  ", "admin");

        var evt = customer.DomainEvents.OfType<OpeningReceivableRecorded>().Single();
        evt.ReferenceNo.Should().Be("SALDO-AWAL");
        evt.Notes.Should().BeNull();
    }

    [Fact]
    public void RecordOpeningReceivable_MultipleEntries_Accumulate()
    {
        var customer = ActiveCustomer();

        customer.RecordOpeningReceivable(Guid.NewGuid(), 100_000m, OldDate, "NOTA-1", null, "admin");
        customer.RecordOpeningReceivable(Guid.NewGuid(), 50_000m, OldDate.AddDays(10), "NOTA-2", null, "admin");

        customer.OutstandingDebt.Should().Be(150_000m);
        customer.OpenReceivables.Should().HaveCount(2);
        customer.DomainEvents.OfType<OpeningReceivableRecorded>().Last().NewBalance.Should().Be(150_000m);
    }

    [Fact]
    public void RecordOpeningReceivable_AddsOnTopOfExistingCreditSaleDebt()
    {
        var customer = ActiveCustomer();
        customer.IncurDebt(80_000m, Guid.NewGuid(), "INV-1", "kasir-01");

        customer.RecordOpeningReceivable(Guid.NewGuid(), 20_000m, OldDate, "NOTA-1", null, "admin");

        customer.OutstandingDebt.Should().Be(100_000m);
    }

    // ── RecordOpeningReceivable: guards ───────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RecordOpeningReceivable_NonPositiveAmount_Throws(decimal amount)
    {
        var customer = ActiveCustomer();

        var act = () => customer.RecordOpeningReceivable(
            Guid.NewGuid(), amount, OldDate, null, null, "admin");

        act.Should().Throw<DomainException>();
        customer.OutstandingDebt.Should().Be(0m);
    }

    [Fact]
    public void RecordOpeningReceivable_AmountAboveCap_Throws()
    {
        var customer = ActiveCustomer();

        var act = () => customer.RecordOpeningReceivable(
            Guid.NewGuid(), Customer.MaxOpeningReceivableAmount + 1, OldDate, null, null, "admin");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordOpeningReceivable_FractionalRupiah_Throws()
    {
        var customer = ActiveCustomer();

        var act = () => customer.RecordOpeningReceivable(
            Guid.NewGuid(), 100_000.5m, OldDate, null, null, "admin");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordOpeningReceivable_DateBeforeMinimum_Throws()
    {
        var customer = ActiveCustomer();

        var act = () => customer.RecordOpeningReceivable(
            Guid.NewGuid(), 100_000m, Customer.MinOpeningReceivableDate.AddDays(-1), null, null, "admin");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordOpeningReceivable_EmptyReceivableId_Throws()
    {
        var customer = ActiveCustomer();

        var act = () => customer.RecordOpeningReceivable(
            Guid.Empty, 100_000m, OldDate, null, null, "admin");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordOpeningReceivable_FutureDate_Throws()
    {
        var customer = ActiveCustomer();

        var act = () => customer.RecordOpeningReceivable(
            Guid.NewGuid(), 100_000m, DateTime.UtcNow.AddDays(3), null, null, "admin");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordOpeningReceivable_TodayInLocalTimeZone_IsAllowed()
    {
        // A WIB (UTC+7) user picking "today" early in the morning sends a date that is
        // already tomorrow in UTC terms; that must not be rejected as a future date.
        var customer = ActiveCustomer();
        var localToday = DateTime.UtcNow.AddHours(7).Date;

        var act = () => customer.RecordOpeningReceivable(
            Guid.NewGuid(), 100_000m, localToday, null, null, "admin");

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordOpeningReceivable_InactiveCustomer_Throws()
    {
        var customer = ActiveCustomer();
        customer.Deactivate("admin");

        var act = () => customer.RecordOpeningReceivable(
            Guid.NewGuid(), 100_000m, OldDate, null, null, "admin");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordOpeningReceivable_DuplicateReceivableId_Throws()
    {
        var customer     = ActiveCustomer();
        var receivableId = Guid.NewGuid();
        customer.RecordOpeningReceivable(receivableId, 100_000m, OldDate, null, null, "admin");

        var act = () => customer.RecordOpeningReceivable(
            receivableId, 100_000m, OldDate, null, null, "admin");

        act.Should().Throw<DomainException>();
        customer.OutstandingDebt.Should().Be(100_000m);
    }

    [Fact]
    public void RecordOpeningReceivable_DuplicateIdOfFullyPaidLine_StillThrows()
    {
        var customer     = ActiveCustomer();
        var receivableId = Guid.NewGuid();
        customer.RecordOpeningReceivable(receivableId, 100_000m, OldDate, null, null, "admin");
        customer.RecordRepayment(100_000m, PaymentMethod.Cash, null, "kasir-01");

        var act = () => customer.RecordOpeningReceivable(
            receivableId, 100_000m, OldDate, null, null, "admin");

        act.Should().Throw<DomainException>();
        customer.OutstandingDebt.Should().Be(0m);
    }

    // ── Interaction with repayment (FIFO) ─────────────────────────────────────

    [Fact]
    public void RecordRepayment_PaysOlderOpeningReceivableBeforeNewerCreditSale()
    {
        var customer = ActiveCustomer();
        customer.IncurDebt(80_000m, Guid.NewGuid(), "INV-NEW", "kasir-01");
        var openingId = Guid.NewGuid();
        customer.RecordOpeningReceivable(openingId, 50_000m, OldDate, "NOTA-LAMA", null, "admin");
        customer.ClearDomainEvents();

        customer.RecordRepayment(60_000m, PaymentMethod.Cash, null, "kasir-01");

        var evt = customer.DomainEvents.OfType<ReceivablePaymentRecorded>().Single();
        evt.Allocations.Should().HaveCount(2);
        evt.Allocations[0].SaleId.Should().Be(openingId);
        evt.Allocations[0].ReferenceNo.Should().Be("NOTA-LAMA");
        evt.Allocations[0].AppliedAmount.Should().Be(50_000m);
        evt.Allocations[0].RemainingAfter.Should().Be(0m);
        evt.Allocations[1].ReferenceNo.Should().Be("INV-NEW");
        evt.Allocations[1].AppliedAmount.Should().Be(10_000m);
        customer.OutstandingDebt.Should().Be(70_000m);
    }

    // ── Replay ────────────────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_ReplaysOpeningReceivableAndPayment()
    {
        var customer  = Customer.Create("Budi Santoso", "08123456789", null, "admin");
        var openingId = Guid.NewGuid();
        customer.RecordOpeningReceivable(openingId, 100_000m, OldDate, "NOTA-LAMA", null, "admin");
        customer.RecordRepayment(30_000m, PaymentMethod.Cash, null, "kasir-01");
        var events = customer.DomainEvents.ToList();

        var replayed = Customer.Reconstitute(events);

        replayed.OutstandingDebt.Should().Be(70_000m);
        var line = replayed.OpenReceivables.Should().ContainSingle().Subject;
        line.SaleId.Should().Be(openingId);
        line.RemainingAmount.Should().Be(70_000m);
        line.IncurredAt.Should().Be(OldDate);
    }
}
