using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Financials;
using Materia.Domain.Financials.Events;

namespace Materia.Tests.Financials;

public class ChangeFundDepositTests
{
    private const string User = "admin";
    private const string PettyCashRef = "KK-20260610-0001";

    // ── PettyCashCategory label ────────────────────────────────────────────────

    [Fact]
    public void TukarUangKembalian_HasCorrectDisplayLabel()
    {
        PettyCashCategory.TukarUangKembalian.ToDisplayLabel()
            .Should().Be("Tukar Uang Kembalian");
    }

    // ── ChangeFundDeposit.Record — happy path ─────────────────────────────────

    [Fact]
    public void Record_ManualEntry_RaisesChangeFundDeposited()
    {
        var deposit = ChangeFundDeposit.Record(
            100_000m, ChangeFundSource.ManualEntry, null, "Persiapan shift pagi", User);

        var evt = deposit.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ChangeFundDeposited>().Subject;

        evt.Amount.Should().Be(100_000m);
        evt.Source.Should().Be(ChangeFundSource.ManualEntry);
        evt.SourceReferenceNo.Should().BeNull();
        evt.Notes.Should().Be("Persiapan shift pagi");
        evt.RecordedBy.Should().Be(User);
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        deposit.Amount.Should().Be(100_000m);
        deposit.Source.Should().Be(ChangeFundSource.ManualEntry);
        deposit.SourceReferenceNo.Should().BeNull();
        deposit.RecordedBy.Should().Be(User);
        deposit.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Record_PettyCashExchange_RaisesChangeFundDeposited_WithSourceRef()
    {
        var deposit = ChangeFundDeposit.Record(
            50_000m, ChangeFundSource.PettyCashExchange, PettyCashRef, null, User);

        var evt = deposit.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ChangeFundDeposited>().Subject;

        evt.Source.Should().Be(ChangeFundSource.PettyCashExchange);
        evt.SourceReferenceNo.Should().Be(PettyCashRef);
        deposit.SourceReferenceNo.Should().Be(PettyCashRef);
    }

    // ── Domain rules ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100_000)]
    public void Record_NonPositiveAmount_ThrowsDomainException(decimal amount)
    {
        Action act = () => ChangeFundDeposit.Record(
            amount, ChangeFundSource.ManualEntry, null, null, User);

        act.Should().Throw<DomainException>().WithMessage("*lebih dari nol*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Record_BlankRecordedBy_ThrowsDomainException(string? recordedBy)
    {
        Action act = () => ChangeFundDeposit.Record(
            50_000m, ChangeFundSource.ManualEntry, null, null, recordedBy!);

        act.Should().Throw<DomainException>().WithMessage("*Pencatat*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_PettyCashExchange_WithoutSourceRef_ThrowsDomainException(string? refNo)
    {
        Action act = () => ChangeFundDeposit.Record(
            50_000m, ChangeFundSource.PettyCashExchange, refNo, null, User);

        act.Should().Throw<DomainException>().WithMessage("*referensi*");
    }

    // ── Amount rounding ───────────────────────────────────────────────────────

    [Fact]
    public void Record_AmountIsRoundedToTwoDecimals()
    {
        var deposit = ChangeFundDeposit.Record(
            99_999.999m, ChangeFundSource.ManualEntry, null, null, User);

        deposit.Amount.Should().Be(Math.Round(99_999.999m, 2));
    }

    // ── Notes normalisation ───────────────────────────────────────────────────

    [Fact]
    public void Record_BlankNotes_StoresNull()
    {
        var deposit = ChangeFundDeposit.Record(
            50_000m, ChangeFundSource.ManualEntry, null, "   ", User);

        deposit.Notes.Should().BeNull();
    }

    // ── Idempotency key ───────────────────────────────────────────────────────

    [Fact]
    public void Record_WithIdempotencyKey_BakesKeyIntoEvent()
    {
        var key = Guid.NewGuid();

        var deposit = ChangeFundDeposit.Record(
            100_000m, ChangeFundSource.ManualEntry, null, null, User, key);

        deposit.DomainEvents.OfType<ChangeFundDeposited>().Single()
            .IdempotencyKey.Should().Be(key);
        deposit.IdempotencyKey.Should().Be(key);
    }

    [Fact]
    public void Record_WithoutIdempotencyKey_GeneratesNonEmptyKey()
    {
        var deposit = ChangeFundDeposit.Record(
            100_000m, ChangeFundSource.ManualEntry, null, null, User);

        deposit.IdempotencyKey.Should().NotBeEmpty();
    }

    [Fact]
    public void Record_WithEmptyIdempotencyKey_GeneratesNonEmptyKey()
    {
        var deposit = ChangeFundDeposit.Record(
            100_000m, ChangeFundSource.ManualEntry, null, null, User, Guid.Empty);

        deposit.IdempotencyKey.Should().NotBeEmpty();
    }

    // ── Reconstitute ─────────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_FromEvents_RestoresStateAndClearsPendingEvents()
    {
        var original = ChangeFundDeposit.Record(
            75_000m, ChangeFundSource.PettyCashExchange, PettyCashRef, "shift sore", User);

        var reconstituted = ChangeFundDeposit.Reconstitute(original.DomainEvents);

        reconstituted.Id.Should().Be(original.Id);
        reconstituted.Amount.Should().Be(75_000m);
        reconstituted.Source.Should().Be(ChangeFundSource.PettyCashExchange);
        reconstituted.SourceReferenceNo.Should().Be(PettyCashRef);
        reconstituted.Notes.Should().Be("shift sore");
        reconstituted.RecordedBy.Should().Be(User);
        reconstituted.IdempotencyKey.Should().Be(original.IdempotencyKey);
        reconstituted.DomainEvents.Should().BeEmpty();
    }
}
